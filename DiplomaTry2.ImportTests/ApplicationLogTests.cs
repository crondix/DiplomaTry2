using DiplomaTry2.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DiplomaTry2.ImportTests;

// Filesystem-only checks: no SQL Server, SQLite, Windows auth or browser required.
public sealed class ApplicationLogTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "DiplomaTry2-log-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Entries_and_exception_details_survive_restart_in_newest_first_order()
    {
        using (var store = new ApplicationLogStore(directory)) {
            var logger = store.CreateLogger("Import");
            logger.LogInformation("Импорт начат");
            logger.LogError(new InvalidOperationException("Сервер недоступен"), "Ошибка импорта");
        }
        using var restored = new ApplicationLogStore(directory);
        var entries = restored.Snapshot().Entries;
        Assert.Equal(2, entries.Count);
        Assert.Equal(LogLevel.Error, entries[0].Level);
        Assert.Contains("Сервер недоступен", entries[0].Exception);
        Assert.Equal("Импорт начат", entries[1].Message);
        Assert.Equal(2, entries.Select(entry => entry.Id).Distinct().Count());
        Assert.Null(restored.Snapshot().StorageError);
    }

    [Fact]
    public void File_provider_respects_configured_category_levels()
    {
        using var services = new ServiceCollection()
            .AddSingleton(_ => new ApplicationLogStore(directory))
            .AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<ApplicationLogStore>())
            .AddLogging(logging => logging.AddFilter("Noise", LogLevel.Warning))
            .BuildServiceProvider();
        var factory = services.GetRequiredService<ILoggerFactory>();
        factory.CreateLogger("Noise").LogInformation("Hidden");
        factory.CreateLogger("Noise").LogWarning("Visible");
        var entry = Assert.Single(services.GetRequiredService<ApplicationLogStore>().Snapshot().Entries);
        Assert.Equal("Visible", entry.Message);
    }

    [Theory]
    [InlineData("Server=db;Password=secret value;Database=print")]
    [InlineData("Pwd=\"secret;value\";User Id=admin")]
    [InlineData("Password='secret''value';Server=db")]
    [InlineData("Authorization: Bearer secret")]
    [InlineData("{\"Password\": \"secret\", \"Server\": \"db\"}")]
    public void Passwords_and_tokens_are_redacted_in_memory_and_on_disk(string text)
    {
        using (var store = new ApplicationLogStore(directory)) {
            store.CreateLogger("Settings").LogError(new Exception(text), "{Detail}", text);
            var entry = Assert.Single(store.Snapshot().Entries);
            Assert.DoesNotContain("secret", entry.Message);
            Assert.DoesNotContain("secret", entry.Exception);
        }
        Assert.DoesNotContain("secret", File.ReadAllText(Assert.Single(Directory.GetFiles(directory))));
        using var restored = new ApplicationLogStore(directory);
        Assert.DoesNotContain("secret", Assert.Single(restored.Snapshot().Entries).Exception);
    }

    [Fact]
    public void Recent_entries_are_bounded_and_restored_despite_a_partial_last_line()
    {
        using (var store = new ApplicationLogStore(directory)) {
            var logger = store.CreateLogger("Test");
            for (var i = 0; i <= ApplicationLogStore.EntryLimit; i++) logger.LogInformation("Entry {Number}", i);
            Assert.Equal(ApplicationLogStore.EntryLimit, store.Snapshot().Entries.Count);
        }
        File.AppendAllText(Assert.Single(Directory.GetFiles(directory)), "{partial");
        using var restored = new ApplicationLogStore(directory);
        Assert.Equal(ApplicationLogStore.EntryLimit, restored.Snapshot().Entries.Count);
        Assert.Equal($"Entry {ApplicationLogStore.EntryLimit}", restored.Snapshot().Entries[0].Message);
        Assert.Equal("Entry 1", restored.Snapshot().Entries[^1].Message);
    }

    [Fact]
    public void Rotation_limits_file_sizes_and_retention_preserves_unrelated_files()
    {
        Directory.CreateDirectory(directory);
        for (var i = 0; i < ApplicationLogStore.FileLimit + 2; i++)
            File.WriteAllText(Path.Combine(directory, $"application-20000101-{i:D8}.jsonl"), "");
        var unrelated = Path.Combine(directory, "keep.txt");
        File.WriteAllText(unrelated, "keep");
        using (var store = new ApplicationLogStore(directory)) {
            var logger = store.CreateLogger("Test");
            var message = new string('a', 16000);
            for (var i = 0; i < 400; i++) logger.LogInformation("{Message}", message);
        }
        var files = Directory.GetFiles(directory, "application-*.jsonl");
        Assert.Equal(ApplicationLogStore.FileLimit, files.Length);
        Assert.Equal(2, files.Count(file => new FileInfo(file).Length > 0));
        Assert.All(files, file => Assert.InRange(new FileInfo(file).Length, 0, ApplicationLogStore.FileSizeLimit));
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public void Unwritable_directory_keeps_logs_in_memory_and_recovers_after_fix()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "blocked");
        File.WriteAllText(path, "file instead of directory");
        using var store = new ApplicationLogStore(path);
        var logger = store.CreateLogger("Test");
        logger.LogError("Still visible");
        Assert.Equal("Still visible", Assert.Single(store.Snapshot().Entries).Message);
        Assert.NotNull(store.Snapshot().StorageError);
        File.Delete(path);
        logger.LogInformation("Recovered");
        Assert.Null(store.Snapshot().StorageError);
        Assert.Single(Directory.GetFiles(path));
    }

    [Fact]
    public void Concurrent_writes_produce_valid_unique_records()
    {
        using (var store = new ApplicationLogStore(directory)) {
            var logger = store.CreateLogger("Parallel");
            Parallel.For(0, 200, i => logger.LogInformation("Entry {Number}", i));
            Assert.Equal(200, store.Snapshot().Entries.Count);
        }
        using var restored = new ApplicationLogStore(directory);
        Assert.Equal(200, restored.Snapshot().Entries.Select(entry => entry.Id).Distinct().Count());
    }

    public void Dispose()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DiplomaTry2-log-tests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test directory is outside the expected temporary root.");
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
