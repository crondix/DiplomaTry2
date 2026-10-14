using DiplomaTry2.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class ServiceDatabaseUpgradeTests
{
    private const string Connection = "Server=preserved;Database=printing;User ID=app;Password=preserved_secret";

    [Fact]
    public void Xpo_upgrade_preserves_live_data_ciphertext_and_ids_without_restoring_deleted_rows()
    {
        string? encrypted = null;
        using var fixture = new StoreFixture(initialize: (path, protection, sid, name) => {
            encrypted = CreateXpoDatabase(path, protection, sid, name);
        });
        var store = fixture.Store;
        Assert.Equal(new GlobalSnapshot(Connection, "saved-print"), store.Snapshot());
        Assert.Equal(encrypted, store.ReadSetting("Connection"));
        Assert.Equal("Complete", store.ReadSetting("LegacyMigration"));
        Assert.Single(store.Accounts(fixture.Sid));
        Assert.True(store.IsAdmin(fixture.Sid));
        Assert.Equal(2, store.PrintServers().Count);
        Assert.True(store.PrintServers().Single(s => s.Host == "saved-print").IsActive);

        using var db = OpenContext(store.Path);
        Assert.Equal(41, db.Accounts.Single().Id);
        Assert.Equal(81, db.PrintServers.Single(s => s.Host == "saved-print").Id);
        Assert.Equal(2, db.Roles.Count());
        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.False(HasColumn(store.Path, "WindowsAccounts", "GCRecord"));
        Assert.True(HasColumn(store.Path + ".xpo-backup", "WindowsAccounts", "GCRecord"));

        store.ChangeAccount(fixture.Sid, "new-user", "new", "User");
        store.RemovePrintServer(fixture.Sid, "other-print");
        using var reopened = fixture.Open();
        Assert.Equal(store.Snapshot(), reopened.Snapshot());
        Assert.Equal(2, reopened.Accounts(fixture.Sid).Count);
        Assert.Single(reopened.PrintServers());
        using var backup = OpenSqlite(store.Path + ".xpo-backup");
        using var command = backup.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM WindowsAccounts";
        Assert.Equal(2L, command.ExecuteScalar());
    }

    [Fact]
    public void Older_xpo_database_without_server_list_keeps_active_server()
    {
        using var fixture = new StoreFixture(initialize: (path, protection, sid, name) => {
            CreateXpoDatabase(path, protection, sid, name, withServers: false);
        });
        var server = Assert.Single(fixture.Store.PrintServers());
        Assert.Equal("saved-print", server.Host);
        Assert.True(server.IsActive);
    }

    [Fact]
    public void Failed_upgrade_rolls_back_schema_and_rows_and_keeps_backup()
    {
        using var fixture = new StoreFixture();
        var legacyPath = System.IO.Path.Combine(fixture.Folder, "invalid-xpo.sqlite");
        CreateXpoDatabase(legacyPath, fixture.Protection, fixture.Sid, fixture.Name);
        using (var sql = OpenSqlite(legacyPath)) {
            using var command = sql.CreateCommand();
            command.CommandText = "UPDATE WindowsAccounts SET GCRecord=NULL";
            command.ExecuteNonQuery();
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ServiceDatabase:Path"] = legacyPath }).Build();
        // Reuse the fixture environment without starting the host or contacting SQL Server.
        Assert.Throws<SqliteException>(() => new ServiceStore(config, fixture.Protection, fixture.Hosting));
        Assert.True(HasColumn(legacyPath, "WindowsAccounts", "Oid"));
        Assert.False(HasColumn(legacyPath, "WindowsAccounts", "Id"));
        Assert.True(System.IO.File.Exists(legacyPath + ".xpo-backup"));
        using var connection = OpenSqlite(legacyPath);
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM WindowsAccounts";
        Assert.Equal(2L, query.ExecuteScalar());
        query.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name='__EFMigrationsHistory'";
        Assert.Equal(0L, query.ExecuteScalar());
    }

    [Fact]
    public void Missing_legacy_column_is_rejected_without_replacing_tables()
    {
        using var fixture = new StoreFixture();
        var legacyPath = System.IO.Path.Combine(fixture.Folder, "incomplete-xpo.sqlite");
        CreateXpoDatabase(legacyPath, fixture.Protection, fixture.Sid, fixture.Name);
        using (var sql = OpenSqlite(legacyPath)) {
            using var command = sql.CreateCommand();
            command.CommandText = "ALTER TABLE WindowsAccounts DROP COLUMN Role";
            command.ExecuteNonQuery();
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ServiceDatabase:Path"] = legacyPath }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceStore(config, fixture.Protection, fixture.Hosting));
        Assert.True(HasColumn(legacyPath, "WindowsAccounts", "Oid"));
        Assert.False(HasColumn(legacyPath, "WindowsAccounts", "Id"));
        Assert.False(System.IO.File.Exists(legacyPath + ".xpo-backup"));
    }

    [Fact]
    public void Failed_global_update_does_not_commit_connection_change()
    {
        using var fixture = new StoreFixture();
        fixture.MakeAdmin();
        var before = fixture.Store.Snapshot();
        Assert.Throws<ArgumentException>(() => fixture.Store.SaveGlobals(fixture.Sid, new(Connection, "invalid/server")));
        Assert.Equal(before, fixture.Store.Snapshot());
    }

    [Fact]
    public async Task Concurrent_first_admin_claim_allows_only_one_winner()
    {
        using var fixture = new StoreFixture();
        fixture.Store.ImportAccounts([]);
        using var other = fixture.Open();
        var results = await Task.WhenAll(
            Task.Run(() => fixture.Store.Claim(fixture.Sid, fixture.Name, null)),
            Task.Run(() => other.Claim("other-sid", "other", null)));
        Assert.Equal(1, results.Count(r => r));
        Assert.True(fixture.Store.HasAdmin());
    }

    private static ServiceDbContext OpenContext(string path) => new(
        new DbContextOptionsBuilder<ServiceDbContext>().UseSqlite(
            new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()).Options);

    private static SqliteConnection OpenSqlite(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path, Pooling = false
        }.ToString());
        connection.Open();
        return connection;
    }

    private static bool HasColumn(string path, string table, string column)
    {
        using var connection = OpenSqlite(path);
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var reader = command.ExecuteReader();
        while (reader.Read()) if (reader.GetString(1) == column) return true;
        return false;
    }

    // Reproduce the former XPObject tables, including deferred deletion and a
    // duplicate SID on a deleted account. The migration must use only live rows.
    private static string CreateXpoDatabase(string path, IDataProtectionProvider protection,
        string sid, string name, bool withServers = true)
    {
        var protector = protection.CreateProtector("DiplomaTry2.ServiceDatabase.Connection.v1");
        var encrypted = protector.Protect(Connection);
        using var connection = OpenSqlite(path);
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE WindowsAccounts (Oid INTEGER PRIMARY KEY AUTOINCREMENT, Sid TEXT, Name TEXT, Role TEXT, GCRecord INTEGER, OptimisticLockField INTEGER);
            CREATE TABLE GlobalSettings (Oid INTEGER PRIMARY KEY AUTOINCREMENT, "Key" TEXT, Value TEXT, GCRecord INTEGER, OptimisticLockField INTEGER);
            CREATE TABLE ApplicationRoles (Oid INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, GCRecord INTEGER, OptimisticLockField INTEGER);
            INSERT INTO WindowsAccounts (Oid, Sid, Name, Role) VALUES (41, $sid, $name, 'Admin');
            INSERT INTO WindowsAccounts (Oid, Sid, Name, Role, GCRecord) VALUES (42, $sid, 'deleted', 'Admin', 123);
            INSERT INTO GlobalSettings ("Key", Value) VALUES ('Initialized', '1'), ('Connection', $encrypted), ('LegacyConnection', $encrypted), ('PrintServer', 'saved-print'), ('LegacyMigration', 'Complete');
            INSERT INTO GlobalSettings ("Key", Value, GCRecord) VALUES ('Connection', 'obsolete-encrypted-value', 123);
            INSERT INTO ApplicationRoles (Name) VALUES ('User'), ('Admin');
            INSERT INTO ApplicationRoles (Name, GCRecord) VALUES ('Admin', 123);
            """;
        if (withServers) command.CommandText += """

            CREATE TABLE PrintServers (Oid INTEGER PRIMARY KEY AUTOINCREMENT, Host TEXT, HostKey TEXT, OptimisticLockField INTEGER);
            INSERT INTO PrintServers (Oid, Host, HostKey) VALUES (81, 'saved-print', 'SAVED-PRINT'), (82, 'other-print', 'OTHER-PRINT');
            INSERT INTO GlobalSettings ("Key", Value) VALUES ('PrintServerListInitialized', '1');
            """;
        command.Parameters.AddWithValue("$sid", sid);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$encrypted", encrypted);
        command.ExecuteNonQuery();
        return encrypted;
    }
}
