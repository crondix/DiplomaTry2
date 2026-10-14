using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DiplomaTry2.Services;

public sealed record ApplicationLogEntry(Guid Id, DateTimeOffset Timestamp, LogLevel Level,
    string Category, int EventId, string Message, string? Exception)
{
    public DateTime LocalTime => Timestamp.LocalDateTime;
    public string LevelName => Level switch {
        LogLevel.Trace => "Трассировка", LogLevel.Debug => "Отладка",
        LogLevel.Information => "Информация", LogLevel.Warning => "Предупреждение",
        LogLevel.Error => "Ошибка", LogLevel.Critical => "Критическая ошибка", _ => Level.ToString()
    };
}

public sealed record ApplicationLogSnapshot(IReadOnlyList<ApplicationLogEntry> Entries,
    string DirectoryPath, string? StorageError);

// Independent of SQL Server and the service database, including during startup failures.
public sealed class ApplicationLogStore : ILoggerProvider
{
    public const int EntryLimit = 2000;
    public const int FileLimit = 10;
    public const long FileSizeLimit = 5 * 1024 * 1024;
    private static readonly Regex Secrets = new(
        "(?i)\\b(password|pwd|access[_-]?token|client[_-]?secret|authorization)[\"']?\\s*[:=]\\s*(\"(?:[^\"]|\"\")*\"|'(?:[^']|'')*'|[^;\\r\\n,}]+)",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private readonly object gate = new();
    private readonly Queue<ApplicationLogEntry> entries = new();
    private readonly string directory;
    private StreamWriter? writer;
    private long fileSize;
    private DateOnly fileDate;
    private string? storageError;
    private bool disposed;

    public ApplicationLogStore(string directoryPath)
    {
        directory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directoryPath));
        try {
            Directory.CreateDirectory(directory);
            PruneFiles(FileLimit);
            foreach (var file in LogFiles().Reverse()) {
                foreach (var line in File.ReadLines(file.FullName)) {
                    try {
                        var entry = JsonSerializer.Deserialize<ApplicationLogEntry>(line);
                        if (entry is not null) Remember(entry);
                    }
                    catch (JsonException) { /* Ignore an incomplete final record after a crash. */ }
                }
            }
        }
        catch (Exception ex) when (IsStorageFailure(ex)) { StorageFailed(ex); }
    }

    public ILogger CreateLogger(string categoryName) => new JournalLogger(this, categoryName);

    public ApplicationLogSnapshot Snapshot()
    {
        lock (gate) return new(entries.OrderByDescending(entry => entry.Timestamp).ToArray(), directory, storageError);
    }

    private void Write(LogLevel level, string category, EventId eventId, string message, Exception? exception)
    {
        var entry = new ApplicationLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, level,
            category, eventId.Id, SafeText(message, 16000), exception is null ? null : SafeText(exception.ToString(), 32000));
        lock (gate) {
            if (disposed) return;
            Remember(entry);
            try {
                var line = JsonSerializer.Serialize(entry);
                var bytes = Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(Environment.NewLine);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (writer is null || today != fileDate || fileSize + bytes > FileSizeLimit) {
                    writer?.Dispose();
                    writer = null;
                    Directory.CreateDirectory(directory);
                    PruneFiles(FileLimit - 1);
                    var path = Path.Combine(directory, $"application-{DateTime.UtcNow:yyyyMMdd-HHmmssfffffff}-{Guid.NewGuid():N}.jsonl");
                    writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
                    fileDate = today;
                    fileSize = 0;
                }
                writer.WriteLine(line);
                fileSize += bytes;
                storageError = null;
            }
            catch (Exception ex) when (IsStorageFailure(ex)) {
                try { writer?.Dispose(); } catch (Exception closeError) when (IsStorageFailure(closeError)) { }
                writer = null;
                StorageFailed(ex);
            }
        }
    }

    private void Remember(ApplicationLogEntry entry)
    {
        entries.Enqueue(entry);
        while (entries.Count > EntryLimit) entries.Dequeue();
    }

    private FileInfo[] LogFiles() => new DirectoryInfo(directory).GetFiles("application-*.jsonl")
        .OrderByDescending(file => file.Name, StringComparer.Ordinal).ToArray();

    private void PruneFiles(int keep)
    {
        foreach (var file in LogFiles().Skip(keep)) file.Delete();
    }

    private void StorageFailed(Exception exception)
    {
        if (storageError is null)
            Console.Error.WriteLine($"Не удалось сохранить журнал приложения ({exception.GetType().Name}). Проверьте каталог логов и права доступа.");
        storageError = "Не удалось сохранить логи на диске. Показаны записи в памяти; проверьте каталог, свободное место и права службы приложения.";
    }

    private static bool IsStorageFailure(Exception ex) => ex is IOException or UnauthorizedAccessException or System.Security.SecurityException;
    private static string SafeText(string value, int limit)
    {
        try { value = Secrets.Replace(value, "$1=***"); }
        catch (RegexMatchTimeoutException) { return "Содержимое скрыто: не удалось проверить секреты."; }
        return value.Length <= limit ? value : value[..limit] + "… [сокращено]";
    }

    public void Dispose()
    {
        lock (gate) {
            if (disposed) return;
            disposed = true;
            try { writer?.Dispose(); } catch (Exception ex) when (IsStorageFailure(ex)) { StorageFailed(ex); }
            writer = null;
        }
    }

    private sealed class JournalLogger(ApplicationLogStore store, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) store.Write(logLevel, category, eventId, formatter(state, exception), exception);
        }
    }
}
