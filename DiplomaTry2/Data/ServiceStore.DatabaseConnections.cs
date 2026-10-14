using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;

namespace DiplomaTry2.Data;

// Only public connection attributes are returned to Blazor; passwords stay in the store.
public sealed record DatabaseConnectionInfo(int Id, string Server, string Database, bool IntegratedSecurity,
    string Login, bool TrustServerCertificate, bool IsActive);

public sealed partial class ServiceStore
{
    private static string ConnectionKey(string connection)
    {
        var cs = new SqlConnectionStringBuilder(connection);
        if (string.IsNullOrWhiteSpace(cs.DataSource) || string.IsNullOrWhiteSpace(cs.InitialCatalog))
            throw new ArgumentException("Укажите сервер и имя основной БД.");
        var identity = JsonSerializer.Serialize(new[] { cs.DataSource.Trim().ToUpperInvariant(),
            cs.InitialCatalog.Trim(), cs.IntegratedSecurity ? "Windows" : "SQL", cs.IntegratedSecurity ? "" : cs.UserID });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private static int? ActiveConnectionId(ServiceDbContext db) =>
        int.TryParse(Get(db, "ActiveDatabaseConnectionId"), out var id) ? id : null;

    private void SaveActiveConnection(ServiceDbContext db, string connection, string? protectedConnection = null)
    {
        // Existing callers may intentionally clear an unconfigured connection.
        if (string.IsNullOrWhiteSpace(connection)) {
            Set(db, "Connection", protector.Protect(connection));
            Set(db, "ActiveDatabaseConnectionId", "");
            return;
        }
        var key = ConnectionKey(connection);
        var row = db.DatabaseConnections.SingleOrDefault(c => c.ConnectionKey == key)
            ?? db.DatabaseConnections.Add(new ServiceDatabaseConnection { ConnectionKey = key }).Entity;
        row.ProtectedConnection = protectedConnection ?? protector.Protect(connection);
        // Obtain the generated ID within the same Access transaction.
        db.SaveChanges();
        SetActiveConnection(db, row);
    }

    private static void SetActiveConnection(ServiceDbContext db, ServiceDatabaseConnection row)
    {
        Set(db, "Connection", row.ProtectedConnection);
        Set(db, "ActiveDatabaseConnectionId", row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public IReadOnlyList<DatabaseConnectionInfo> DatabaseConnections() => Access(db => {
        var activeId = ActiveConnectionId(db);
        return db.DatabaseConnections.ToList().Select(row => {
            var cs = new SqlConnectionStringBuilder(protector.Unprotect(row.ProtectedConnection));
            return new DatabaseConnectionInfo(row.Id, cs.DataSource, cs.InitialCatalog, cs.IntegratedSecurity,
                cs.UserID, cs.TrustServerCertificate, row.Id == activeId);
        }).OrderByDescending(c => c.IsActive).ThenBy(c => c.Database).ThenBy(c => c.Server).ToArray();
    });

    internal (string ConnectionString, string Version) ReadDatabaseConnection(string actor, int id) => Access(db => {
        DemandAdmin(db, actor);
        var row = FindConnection(db, id);
        return (protector.Unprotect(row.ProtectedConnection), row.ProtectedConnection);
    });

    public int SaveDatabaseConnection(string actor, int? id, string connection)
    {
        var activeChanged = false;
        var savedId = Access(db => {
            DemandAdmin(db, actor);
            var key = ConnectionKey(connection);
            var duplicate = db.DatabaseConnections.SingleOrDefault(c => c.ConnectionKey == key);
            if (duplicate is not null && duplicate.Id != id)
                throw new InvalidOperationException("Такое подключение уже есть в списке. Измените сохранённое подключение.");
            var row = id.HasValue ? FindConnection(db, id.Value)
                : db.DatabaseConnections.Add(new ServiceDatabaseConnection()).Entity;
            row.ConnectionKey = key;
            row.ProtectedConnection = protector.Protect(connection);
            db.SaveChanges();
            if (row.Id == ActiveConnectionId(db)) {
                SetActiveConnection(db, row);
                activeChanged = true;
            }
            return row.Id;
        });
        if (activeChanged) NotifyChanged();
        return savedId;
    }

    // The version ensures that the connection being activated is exactly the one validated.
    internal void ActivateDatabaseConnection(string actor, int id, string expectedVersion)
    {
        Access(db => {
            DemandAdmin(db, actor);
            var row = FindConnection(db, id);
            if (row.ProtectedConnection != expectedVersion)
                throw new InvalidOperationException("Подключение изменилось во время проверки. Повторите переключение.");
            SetActiveConnection(db, row);
            return true;
        });
        NotifyChanged();
    }

    public void RemoveDatabaseConnection(string actor, int id) => Access(db => {
        DemandAdmin(db, actor);
        if (id == ActiveConnectionId(db))
            throw new InvalidOperationException("Активную БД удалить нельзя. Сначала выберите другую БД.");
        db.DatabaseConnections.Remove(FindConnection(db, id));
        return true;
    });

    private static ServiceDatabaseConnection FindConnection(ServiceDbContext db, int id) =>
        db.DatabaseConnections.SingleOrDefault(c => c.Id == id)
        ?? throw new InvalidOperationException("Подключение отсутствует в списке. Обновите настройки.");
}
