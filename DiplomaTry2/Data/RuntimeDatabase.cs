using System.Security.Principal;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DiplomaTry2.Data;

public sealed class RuntimeDatabase(ServiceStore store) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => Create(store.Snapshot().ConnectionString);
    public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    public static ApplicationDbContext Create(string connection)
    {
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Основная БД не настроена. Администратор может задать подключение на странице /settings.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection, s => s.CommandTimeout(15)).Options);
    }
    public GlobalSnapshot Snapshot() => store.Snapshot();
    public IDbContextFactory<ApplicationDbContext> Capture(GlobalSnapshot? snapshot = null) => new FrozenFactory((snapshot ?? store.Snapshot()).ConnectionString);
    private sealed class FrozenFactory(string connection) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => Create(connection);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    public static async Task ValidateAsync(string connection)
    {
        await using var db = Create(connection);
        await db.Database.OpenConnectionAsync();
        // Read every mapped printing column with TOP(0); do not require Identity tables
        // in a newly selected printing database and never create/alter its schema.
        foreach (var entity in db.Model.GetEntityTypes().Where(e => e.ClrType.Namespace == "DiplomaModels" || e.ClrType == typeof(PrintEventImportCursor)))
        {
            var table = entity.GetTableName()!;
            var schema = entity.GetSchema() ?? "dbo";
            var id = StoreObjectIdentifier.Table(table, entity.GetSchema());
            static string Quote(string s) => "[" + s.Replace("]", "]]") + "]";
            var columns = entity.GetProperties().Select(p => Quote(p.GetColumnName(id)!)).Distinct();
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT TOP (0) {string.Join(",", columns)} FROM {Quote(schema)}.{Quote(table)}";
            command.CommandTimeout = 15;
            await command.ExecuteNonQueryAsync();
            await using var metadata = db.Database.GetDbConnection().CreateCommand();
            metadata.CommandText = "SELECT c.name, TYPE_NAME(c.user_type_id), c.max_length, c.is_nullable FROM sys.columns c WHERE c.object_id = OBJECT_ID(@Table)";
            var parameter = metadata.CreateParameter(); parameter.ParameterName = "@Table"; parameter.Value = schema + "." + table; metadata.Parameters.Add(parameter);
            var actual = new Dictionary<string, (string Type, int Length, bool Nullable)>();
            await using (var reader = await metadata.ExecuteReaderAsync())
                while (await reader.ReadAsync()) actual.Add(reader.GetString(0), (reader.GetString(1), reader.GetInt16(2), reader.GetBoolean(3)));
            foreach (var property in entity.GetProperties()) {
                var column = actual[property.GetColumnName(id)!];
                var expected = property.GetRelationalTypeMapping().StoreType.ToLowerInvariant();
                var baseType = expected.Split('(')[0];
                if (column.Type != baseType || (!property.IsColumnNullable(id) && column.Nullable))
                    throw new InvalidOperationException($"Несовместимый столбец {schema}.{table}.{property.GetColumnName(id)}: ожидался {expected}, получен {column.Type}.");
                if (baseType is "nvarchar" or "varchar" or "varbinary") {
                    var sizeText = expected.Contains('(') ? expected[(expected.IndexOf('(') + 1)..].TrimEnd(')') : "max";
                    var length = int.TryParse(sizeText, out var size) ? size : -1;
                    if (length > 0 && baseType == "nvarchar") length *= 2;
                    if (column.Length != length) throw new InvalidOperationException($"Размер {schema}.{table}.{property.GetColumnName(id)}: ожидалось {length}, получено {column.Length}.");
                }
            }
        }
        // Import idempotency depends on these unique indexes, not just column presence.
        await using var indexes = db.Database.GetDbConnection().CreateCommand();
        indexes.CommandText = """
            SELECT OBJECT_NAME(i.object_id), i.filter_definition,
                STRING_AGG(CONVERT(nvarchar(max), c.name), ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.is_unique=1 AND i.is_disabled=0 AND OBJECT_SCHEMA_NAME(i.object_id)='dbo'
            GROUP BY i.object_id, i.index_id, i.filter_definition
            """;
        var keys = new HashSet<string>();
        await using (var reader = await indexes.ExecuteReaderAsync()) {
            while (await reader.ReadAsync()) {
                var table = reader.GetString(0); var filter = reader.IsDBNull(1) ? "" : reader.GetString(1).Replace(" ", "").Replace("(", "").Replace(")", "").ToUpperInvariant();
                if (filter.Length == 0 || (table == "EventsSuccessfulPrinting" && filter == "[RECORDID]ISNOTNULL") ||
                    (table == "NetworkPrinters" && filter == "[SOURCEKEY]ISNOTNULLAND[QUEUEKEY]ISNOTNULL"))
                    keys.Add(table + ":" + reader.GetString(2));
            }
        }
        if (!new[] {"NetworkPrinters:SourceKey,QueueKey", "EventsSuccessfulPrinting:SourceKey,LogName,RecordId,DateTime", "PrintEventImportCursors:SourceKey,LogName"}.All(keys.Contains))
            throw new InvalidOperationException("В основной БД отсутствуют необходимые индексы импорта.");
    }
}

// Retries only the original legacy connection. Switching the main database cannot
// import another database's administrators. A completed import is never replayed.
public sealed class LegacyRoleMigration(ServiceStore store, ILogger<LegacyRoleMigration> logger) : BackgroundService
{
    public async Task TryImportAsync(CancellationToken token = default)
    {
        if (store.ReadSetting("LegacyMigration") == "Complete") return;
        var connection = store.LegacyConnection();
        if (string.IsNullOrWhiteSpace(connection)) { store.ImportAccounts([]); return; }
        var cs = new SqlConnectionStringBuilder(connection) { ConnectTimeout = 5 };
        await using var sql = new SqlConnection(cs.ConnectionString);
        await sql.OpenAsync(token);
        await using var command = sql.CreateCommand();
        command.CommandText = "SELECT u.Id, u.UserName, r.NormalizedName FROM AspNetUsers u LEFT JOIN AspNetUserRoles ur ON ur.UserId=u.Id LEFT JOIN AspNetRoles r ON r.Id=ur.RoleId";
        command.CommandTimeout = 15;
        var rows = new List<AccountInfo>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var sid = reader.GetString(0); var name = reader.IsDBNull(1) ? sid : reader.GetString(1);
            if (!sid.StartsWith("S-", StringComparison.Ordinal)) {
                try { sid = ((SecurityIdentifier)new NTAccount(name).Translate(typeof(SecurityIdentifier))).Value; }
                catch (IdentityNotMappedException) { throw new InvalidOperationException("Не удалось разрешить Windows SID старой учётной записи; перенос не завершён."); }
            }
            _ = new SecurityIdentifier(sid);
            rows.Add(new(sid, name, !reader.IsDBNull(2) && reader.GetString(2) == "ADMIN" ? "Admin" : "User"));
        }
        store.ImportAccounts(rows);
        logger.LogInformation("Перенос назначений Windows завершён: {Count} записей. Исходная БД сохранена.", rows.Count);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && store.ReadSetting("LegacyMigration") != "Complete")
        {
            try { await TryImportAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch { logger.LogWarning("Перенос ролей ожидает доступной старой БД и разрешения Windows SID. Настройки доступны независимо от основной БД."); }
            if (store.ReadSetting("LegacyMigration") != "Complete") await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

