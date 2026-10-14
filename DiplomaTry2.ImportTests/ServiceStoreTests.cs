using System.Security.Claims;
using System.Security.Principal;
using DiplomaTry2.Data;
using DiplomaTry2.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class StoreFixture : IDisposable
{
    public string Folder { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DiplomaServiceTests", Guid.NewGuid().ToString("N"));
    public IConfiguration Config { get; }
    public IDataProtectionProvider Protection { get; }
    public ServiceStore Store { get; }
    public IWebHostEnvironment Hosting { get; } = new FakeEnvironment();
    public string Sid { get; } = WindowsIdentity.GetCurrent().User!.Value;
    public string Name { get; } = WindowsIdentity.GetCurrent().Name;
    public StoreFixture(string? legacy = null, Action<string, IDataProtectionProvider, string, string>? initialize = null) {
        System.IO.Directory.CreateDirectory(Folder);
        Protection = DataProtectionProvider.Create(new System.IO.DirectoryInfo(System.IO.Path.Combine(Folder, "keys")), b => b.SetApplicationName("DiplomaTests").ProtectKeysWithDpapi());
        Config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ServiceDatabase:Path"] = System.IO.Path.Combine(Folder, "service.sqlite"),
            ["PrintSource:ServerName"] = "initial-print", ["ConnectionStrings:DefaultConnection"] = legacy ?? ""
        }).Build();
        initialize?.Invoke(Config["ServiceDatabase:Path"]!, Protection, Sid, Name);
        Store = Open();
    }
    public ServiceStore Open() => new(Config, Protection, Hosting);
    public ApplicationSettingsService Settings(bool admin = true, PrintServerProbe? probe = null, IMainDatabaseValidator? validator = null) => new(Store, new TestAuthentication(admin ? new ClaimsPrincipal(WindowsIdentity.GetCurrent()) : new ClaimsPrincipal(new ClaimsIdentity())), probe ?? new PrintServerProbe(new PrintServerNetworkProbe()), databaseValidator: validator);
    public void MakeAdmin() { Store.ImportAccounts([new(Sid, Name, "Admin")]); }
    public void Dispose() => Store.Dispose();
    private sealed class FakeEnvironment : IWebHostEnvironment {
        public string ApplicationName { get; set; } = "DiplomaTests";
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = System.IO.Path.Combine(Environment.CurrentDirectory, "app");
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class TestAuthentication(ClaimsPrincipal user) : AuthenticationStateProvider {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}

public sealed class ServiceStoreTests
{
    [Fact] public void Settings_and_encrypted_connection_survive_reopening() {
        using var f = new StoreFixture(); f.MakeAdmin();
        f.Store.SaveGlobals(f.Sid, new("Server=test;Database=test;User ID=test;Password=secret_test_871;", "global-print"));
        using var reopened = f.Open();
        Assert.Equal(f.Store.Snapshot(), reopened.Snapshot());
        Assert.True(reopened.IsAdmin(f.Sid));
        Assert.DoesNotContain("secret_test_871", f.Store.ReadSetting("Connection"));
    }
    [Fact] public void Last_admin_is_protected_and_migration_does_not_regrant_removed_roles() {
        using var f = new StoreFixture(); f.MakeAdmin();
        Assert.Throws<InvalidOperationException>(() => f.Store.ChangeAccount(f.Sid, f.Sid, f.Name, "User"));
        Assert.Throws<InvalidOperationException>(() => f.Store.ChangeAccount(f.Sid, f.Sid, f.Name, null));
        f.Store.ChangeAccount(f.Sid, "S-1-5-21-1-2-3-1000", "test-admin", "Admin");
        f.Store.ChangeAccount(f.Sid, "S-1-5-21-1-2-3-1000", "test-admin", "User");
        f.Store.ImportAccounts([new("S-1-5-21-1-2-3-1000", "test-admin", "Admin")]);
        Assert.False(f.Store.IsAdmin("S-1-5-21-1-2-3-1000"));
        Assert.Equal(2, f.Store.Accounts(f.Sid).Count);
    }
    [Fact] public void Pending_legacy_migration_blocks_open_bootstrap_but_allows_explicit_sid() {
        using var f = new StoreFixture("Server=unavailable;Database=legacy;Integrated Security=true");
        Assert.Throws<InvalidOperationException>(() => f.Store.Claim(f.Sid, f.Name, null));
        Assert.True(f.Store.Claim(f.Sid, f.Name, f.Sid));
        Assert.False(f.Store.Claim("S-1-5-21-1-2-3-1000", "other", f.Sid));
        f.Store.ImportAccounts([new(f.Sid, f.Name, "Admin"), new(f.Sid, f.Name, "User")]);
        Assert.Single(f.Store.Accounts(f.Sid)); Assert.True(f.Store.IsAdmin(f.Sid));
    }
    [Fact] public void Legacy_user_with_both_roles_is_imported_once_with_admin_precedence() {
        using var f = new StoreFixture();
        f.Store.ImportAccounts([new(f.Sid, f.Name, "User"), new(f.Sid, f.Name, "Admin"), new(f.Sid, f.Name, "User")]);
        Assert.True(f.Store.IsAdmin(f.Sid)); Assert.Single(f.Store.Accounts(f.Sid));
    }
    [Fact] public async Task Concurrent_demotion_through_separate_store_instances_keeps_one_admin() {
        using var f = new StoreFixture(); f.MakeAdmin(); using var other = f.Open();
        var sid = "S-1-5-21-1-2-3-1000";
        f.Store.ChangeAccount(f.Sid, sid, "another", "Admin");
        var results = await Task.WhenAll(Task.Run(() => TryDemote(f.Store, f.Sid, f.Sid, f.Name)), Task.Run(() => TryDemote(other, f.Sid, sid, "another")));
        Assert.Equal(1, results.Count(success => success));
        Assert.True(f.Store.HasAdmin());
        static bool TryDemote(ServiceStore store, string actor, string target, string name) {
            try { store.ChangeAccount(actor, target, name, "User"); return true; }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException) { return false; }
        }
    }
    [Fact] public async Task User_and_revoked_admin_cannot_write() {
        using var f = new StoreFixture();
        f.Store.Register(f.Sid, f.Name);
        Assert.Throws<UnauthorizedAccessException>(() => f.Store.SaveGlobals(f.Sid, new("", "evil")));
        var service = f.Settings();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SavePrintServerAsync("evil"));
        f.MakeAdmin();
        f.Store.ChangeAccount(f.Sid, "S-1-5-21-1-2-3-1000", "another", "Admin");
        f.Store.ChangeAccount(f.Sid, f.Sid, f.Name, "User");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SavePrintServerAsync("evil"));
    }
    [Fact] public async Task Invalid_connection_preserves_working_settings_and_global_source_updates_all_scopes() {
        using var f = new StoreFixture(); f.MakeAdmin();
        var service = f.Settings();
        var one = new PrintSourceSelection(new(), f.Store); var two = new PrintSourceSelection(new(), f.Store);
        var captured = one.Capture();
        await service.SavePrintServerAsync("new-print");
        Assert.Equal("new-print", one.ServerName); Assert.Equal(one.ServerName, two.ServerName);
        Assert.Equal("initial-print", captured.ServerName);
        one.Apply("Windows", "obsolete-browser-server", "Default"); Assert.Equal("new-print", one.ServerName);
        var before = f.Store.Snapshot();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveConnectionAsync(new("127.0.0.1,1", "missing", true, "", null)));
        Assert.Equal(before, f.Store.Snapshot());
    }
    [Fact] public void New_database_contexts_use_current_connection_while_captured_factory_keeps_old() {
        using var f = new StoreFixture(); f.MakeAdmin();
        f.Store.SaveConnection(f.Sid, "Server=first;Database=first;Integrated Security=true");
        var runtime = new RuntimeDatabase(f.Store); var captured = runtime.Capture();
        f.Store.SaveConnection(f.Sid, "Server=second;Database=second;Integrated Security=true");
        using var old = captured.CreateDbContext(); using var current = runtime.CreateDbContext();
        Assert.Equal("first", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetConnectionString(old.Database)).DataSource);
        Assert.Equal("second", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetConnectionString(current.Database)).DataSource);
    }
    [Fact] public async Task Frozen_event_and_printer_sources_ignore_subsequent_selection_changes() {
        var selection = new PrintSourceSelection(new() { Mode="Fixture", Scenario="Default", ServerName="test" });
        var events = new SelectedPrintEventSource(selection, new(selection), new(selection)).Capture();
        var printers = new SelectedPrinterSource(selection, new(selection), new(selection)).Capture(selection.Capture());
        selection.Apply("Fixture", "other", "Error");
        Assert.NotEmpty((await events.ReadBatchAsync(null, null, 500)).Events);
        Assert.NotEmpty(await printers.GetModelsAsync()); Assert.NotEmpty(await printers.GetPrintersAsync());
    }
    [Fact] public async Task Legacy_roles_import_from_original_sql_database_without_modification() {
        var connection = Environment.GetEnvironmentVariable("DIPLOMA_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;
        using var f = new StoreFixture(connection);
        var migration = new LegacyRoleMigration(f.Store, NullLogger<LegacyRoleMigration>.Instance);
        await migration.TryImportAsync();
        Assert.Equal("Complete", f.Store.ReadSetting("LegacyMigration"));
        Assert.True(f.Store.HasAdmin());
        Assert.True(f.Store.IsAdmin(f.Sid));
        await migration.TryImportAsync();
    }
    [Fact] public async Task Existing_printing_schema_passes_read_only_compatibility_check() {
        var connection = Environment.GetEnvironmentVariable("DIPLOMA_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;
        await RuntimeDatabase.ValidateAsync(connection);
        var incompatible = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection) { InitialCatalog = "master" };
        await Assert.ThrowsAnyAsync<Exception>(() => RuntimeDatabase.ValidateAsync(incompatible.ConnectionString));
    }
}

