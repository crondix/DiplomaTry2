using DiplomaTry2.Data;
using DiplomaTry2.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class DatabaseConnectionTests
{
    private const string Initial = "Server=first;Database=printing;Integrated Security=true;Encrypt=true;TrustServerCertificate=true";

    [Fact]
    public void Existing_connection_is_added_to_list_once_and_keeps_original_settings()
    {
        using var fixture = new StoreFixture(Initial);
        var item = Assert.Single(fixture.Store.DatabaseConnections());
        Assert.True(item.IsActive);
        Assert.True(item.TrustServerCertificate);
        Assert.Equal("first", item.Server);
        Assert.Equal("printing", item.Database);
        Assert.Equal(Initial, fixture.Store.Snapshot().ConnectionString);
        using var reopened = fixture.Open();
        Assert.Equal(item, Assert.Single(reopened.DatabaseConnections()));
    }

    [Fact]
    public void Existing_ef_service_database_is_migrated_without_resetting_connection_or_accounts()
    {
        using var fixture = new StoreFixture(initialize: (path, protection, sid, name) => {
            using var db = OpenContext(path);
            db.GetService<IMigrator>().Migrate(ServiceDatabaseUpgrade.InitialMigration);
            var encrypted = protection.CreateProtector("DiplomaTry2.ServiceDatabase.Connection.v1").Protect(Initial);
            db.Settings.AddRange(new ServiceSetting { Key = "Initialized", Value = "1" },
                new ServiceSetting { Key = "Connection", Value = encrypted },
                new ServiceSetting { Key = "LegacyConnection", Value = encrypted },
                new ServiceSetting { Key = "LegacyMigration", Value = "Complete" });
            db.Accounts.Add(new ServiceAccount { Sid = sid, Name = name, Role = "Admin" });
            db.SaveChanges();
        });
        Assert.True(fixture.Store.IsAdmin(fixture.Sid));
        Assert.Equal(Initial, fixture.Store.Snapshot().ConnectionString);
        Assert.True(Assert.Single(fixture.Store.DatabaseConnections()).IsActive);
        using var reopened = fixture.Open();
        Assert.Single(reopened.DatabaseConnections());
        using var context = OpenContext(fixture.Store.Path);
        Assert.Empty(context.Database.GetPendingMigrations());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Adding_does_not_switch_database_and_activation_preserves_captured_operations()
    {
        using var fixture = new StoreFixture(Initial); fixture.MakeAdmin();
        var validator = new Validator(); var service = fixture.Settings(validator: validator);
        var runtime = new RuntimeDatabase(fixture.Store); var captured = runtime.Capture();
        var changed = 0; fixture.Store.Changed += () => changed++;
        var id = await service.SaveDatabaseConnectionAsync(new("second", "printing2", false, "app", "secret-two", true));
        Assert.Equal(Initial, fixture.Store.Snapshot().ConnectionString);
        Assert.Equal(0, changed);
        Assert.Equal(2, service.DatabaseConnections().Count);
        await service.ActivateDatabaseConnectionAsync(id);
        Assert.Equal(2, validator.Connections.Count);
        Assert.Equal(1, changed);
        var active = new SqlConnectionStringBuilder(fixture.Store.Snapshot().ConnectionString);
        Assert.Equal("second", active.DataSource);
        Assert.Equal("secret-two", active.Password);
        Assert.True(active.TrustServerCertificate);
        using var old = captured.CreateDbContext(); using var current = runtime.CreateDbContext();
        Assert.Equal("first", new SqlConnectionStringBuilder(old.Database.GetConnectionString()).DataSource);
        Assert.Equal("second", new SqlConnectionStringBuilder(current.Database.GetConnectionString()).DataSource);
        using var reopened = fixture.Open();
        Assert.Equal(id, Assert.Single(reopened.DatabaseConnections().Where(c => c.IsActive)).Id);
        Assert.Equal(fixture.Store.Snapshot(), reopened.Snapshot());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveDatabaseConnectionAsync(id));
        var inactive = service.DatabaseConnections().Single(c => !c.IsActive);
        await service.RemoveDatabaseConnectionAsync(inactive.Id);
        Assert.Single(service.DatabaseConnections());
        using var db = OpenContext(fixture.Store.Path);
        Assert.DoesNotContain("secret-two", db.DatabaseConnections.Single().ProtectedConnection);
        Assert.DoesNotContain("secret-two", service.DatabaseConnections().Single().ToString());
    }

    [Fact]
    public async Task Editing_uses_selected_profile_password_and_only_updates_active_connection_when_selected()
    {
        using var fixture = new StoreFixture("Server=first;Database=printing;User ID=app;Password=active-password"); fixture.MakeAdmin();
        var validator = new Validator(); var service = fixture.Settings(validator: validator);
        var id = await service.SaveDatabaseConnectionAsync(new("first", "other", false, "app", "other-password"));
        var before = fixture.Store.Snapshot();
        await service.SaveDatabaseConnectionAsync(new("first", "other", false, "app", null, true), id);
        Assert.Equal("other-password", new SqlConnectionStringBuilder(validator.Connections.Last()).Password);
        Assert.Equal(before, fixture.Store.Snapshot());
        await service.ActivateDatabaseConnectionAsync(id);
        await service.SaveDatabaseConnectionAsync(new("first", "other", false, "app", null, false), id);
        var active = new SqlConnectionStringBuilder(fixture.Store.Snapshot().ConnectionString);
        Assert.False(active.TrustServerCertificate);
        Assert.Equal("other-password", active.Password);
        Assert.Equal(2, service.DatabaseConnections().Count);
    }

    [Fact]
    public async Task Failed_checks_do_not_add_edit_or_activate_connections()
    {
        using var fixture = new StoreFixture(Initial); fixture.MakeAdmin();
        var validator = new Validator(); var service = fixture.Settings(validator: validator);
        var id = await service.SaveDatabaseConnectionAsync(new("second", "other", true, "", null));
        var before = fixture.Store.Snapshot(); var entries = service.DatabaseConnections().ToArray();
        validator.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateDatabaseConnectionAsync(id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveDatabaseConnectionAsync(new("third", "other", true, "", null)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveDatabaseConnectionAsync(new("second", "other", true, "", null, true), id));
        Assert.Equal(before, fixture.Store.Snapshot());
        Assert.Equal(entries, service.DatabaseConnections().ToArray());
    }

    [Fact]
    public async Task Duplicate_identity_is_rejected_but_different_login_can_be_saved()
    {
        using var fixture = new StoreFixture(Initial); fixture.MakeAdmin();
        var service = fixture.Settings(validator: new Validator());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveDatabaseConnectionAsync(new(" FIRST ", "printing", true, "", null)));
        Assert.Single(service.DatabaseConnections());
        await service.SaveDatabaseConnectionAsync(new("first", "printing", false, "app", "password"));
        Assert.Equal(2, service.DatabaseConnections().Count);
        Assert.Equal(Initial, fixture.Store.Snapshot().ConnectionString);
    }

    [Fact]
    public async Task User_and_revoked_admin_cannot_manage_or_check_database_connections()
    {
        using var fixture = new StoreFixture(Initial);
        fixture.Store.Register(fixture.Sid, fixture.Name);
        var validator = new Validator(); var service = fixture.Settings(validator: validator);
        var id = Assert.Single(service.DatabaseConnections()).Id;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveDatabaseConnectionAsync(new("second", "printing", true, "", null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ActivateDatabaseConnectionAsync(id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CheckDatabaseConnectionAsync(id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RemoveDatabaseConnectionAsync(id));
        Assert.Empty(validator.Connections);
        fixture.MakeAdmin();
        var second = await service.SaveDatabaseConnectionAsync(new("second", "other", true, "", null));
        validator.OnValidate = _ => {
            fixture.Store.ChangeAccount(fixture.Sid, "other-admin", "other", "Admin");
            fixture.Store.ChangeAccount(fixture.Sid, fixture.Sid, fixture.Name, "User");
        };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ActivateDatabaseConnectionAsync(second));
        Assert.Equal(Initial, fixture.Store.Snapshot().ConnectionString);
    }

    [Fact]
    public async Task Connection_modified_during_validation_cannot_be_activated()
    {
        using var fixture = new StoreFixture(Initial); fixture.MakeAdmin();
        var validator = new Validator(); var service = fixture.Settings(validator: validator);
        var id = await service.SaveDatabaseConnectionAsync(new("second", "other", true, "", null));
        validator.OnValidate = _ => fixture.Store.SaveDatabaseConnection(fixture.Sid, id,
            "Server=second;Database=changed;Integrated Security=true");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateDatabaseConnectionAsync(id));
        Assert.Equal(Initial, fixture.Store.Snapshot().ConnectionString);
    }

    [Fact]
    public async Task Missing_or_deleted_profile_cannot_be_activated_or_edited()
    {
        using var fixture = new StoreFixture(Initial); fixture.MakeAdmin();
        var validator = new Validator(); var service = fixture.Settings(validator: validator);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateDatabaseConnectionAsync(int.MaxValue));
        var id = await service.SaveDatabaseConnectionAsync(new("second", "other", true, "", null));
        validator.OnValidate = _ => fixture.Store.RemoveDatabaseConnection(fixture.Sid, id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateDatabaseConnectionAsync(id));
        Assert.Single(service.DatabaseConnections());
        Assert.Equal(Initial, fixture.Store.Snapshot().ConnectionString);
    }

    private static ServiceDbContext OpenContext(string path) => new(
        new DbContextOptionsBuilder<ServiceDbContext>().UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder {
            DataSource = path, Pooling = false
        }.ToString()).Options);

    private sealed class Validator : IMainDatabaseValidator
    {
        public List<string> Connections { get; } = [];
        public bool Fail { get; set; }
        public Action<string>? OnValidate { get; set; }
        public Task ValidateAsync(string connection) {
            Connections.Add(connection); OnValidate?.Invoke(connection);
            if (Fail) throw new InvalidOperationException("Тестовая БД недоступна.");
            return Task.CompletedTask;
        }
    }
}
