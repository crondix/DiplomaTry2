using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiplomaTry2.Data;

internal static class ServiceDatabaseUpgrade
{
    internal const string InitialMigration = "20261013102106_ServiceStoreInitial";

    // Called under ServiceStore's cross-process mutex before any EF queries.
    internal static void Initialize(ServiceDbContext db, string path)
    {
        db.Database.OpenConnection();
        try {
            var connection = (SqliteConnection)db.Database.GetDbConnection();
            if (Columns(connection, "WindowsAccounts").Contains("Oid"))
                ConvertXpo(db, connection, path);
            db.Database.Migrate();
        }
        finally { db.Database.CloseConnection(); }
    }

    private static HashSet<string> Columns(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        // Table names are constants owned by this class.
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var reader = command.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) columns.Add(reader.GetString(1));
        return columns;
    }

    private static void ConvertXpo(ServiceDbContext db, SqliteConnection connection, string path)
    {
        var tables = new[] {
            (Name: "WindowsAccounts", Fields: new[] { "Sid", "Name", "Role" }),
            (Name: "GlobalSettings", Fields: new[] { "Key", "Value" }),
            (Name: "ApplicationRoles", Fields: new[] { "Name" }),
            (Name: "PrintServers", Fields: new[] { "Host", "HostKey" })
        };
        var existing = tables.Select(t => (t.Name, t.Fields, Columns: Columns(connection, t.Name)))
            .Where(t => t.Columns.Count > 0).ToArray();
        if (existing.Any(t => !t.Columns.Contains("Oid")))
            throw new InvalidOperationException("Служебная БД содержит смешанную схему XPO/EF. Требуется проверка перед переносом.");
        // Older releases may lack PrintServers, but the other three tables are required.
        if (tables.Take(3).Any(t => !existing.Any(e => e.Name == t.Name)) ||
            existing.Any(t => t.Fields.Any(f => !t.Columns.Contains(f))))
            throw new InvalidOperationException("В служебной БД XPO отсутствуют обязательные таблицы или столбцы. Перенос остановлен без изменения данных.");

        // SQLite's backup API includes committed WAL contents. Never overwrite a prior backup.
        var backupPath = path + ".xpo-backup";
        if (File.Exists(backupPath))
            throw new InvalidOperationException($"Найдена резервная копия незавершённого переноса: {backupPath}. Проверьте её и переместите перед повторной попыткой.");
        using (var backup = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = backupPath, Pooling = false
        }.ToString())) {
            backup.Open();
            connection.BackupDatabase(backup);
        }

        // Staging tables, schema replacement, row copies and migration history are atomic.
        // Soft-deleted XPO accounts/settings/roles must never be resurrected.
        using var transaction = db.Database.BeginTransaction();
        foreach (var table in existing) {
            var filter = table.Columns.Contains("GCRecord") ? " WHERE \"GCRecord\" IS NULL" : "";
            var fields = string.Join(", ", table.Fields.Select(f => $"\"{f}\""));
            var stagingSql = $"CREATE TEMP TABLE \"__Xpo_{table.Name}\" AS SELECT \"Oid\" AS \"Id\", {fields} FROM \"{table.Name}\"{filter}; DROP TABLE \"{table.Name}\";";
            db.Database.ExecuteSqlRaw(stagingSql);
        }
        // Run the fixed initial migration's SQL, not today's model: later migrations
        // (including data migrations) still run normally after this conversion.
        var script = db.GetService<IMigrator>().GenerateScript(Migration.InitialDatabase,
            InitialMigration, MigrationsSqlGenerationOptions.NoTransactions);
        db.Database.ExecuteSqlRaw(script);
        foreach (var table in existing) {
            var fields = string.Join(", ", table.Fields.Select(f => $"\"{f}\""));
            var copySql = $"INSERT INTO \"{table.Name}\" (\"Id\", {fields}) SELECT \"Id\", {fields} FROM \"__Xpo_{table.Name}\"; DROP TABLE \"__Xpo_{table.Name}\";";
            db.Database.ExecuteSqlRaw(copySql);
        }
        transaction.Commit();
    }
}
