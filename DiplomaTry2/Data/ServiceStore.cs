using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.DataProtection;

namespace DiplomaTry2.Data;

public sealed record PrintServerInfo(string Host, bool IsActive);

public sealed record AccountInfo(string Sid, string Name, string Role);
public sealed record GlobalSnapshot(string ConnectionString, string PrintServer);

// All access uses fresh EF contexts. A named mutex serializes writes (including the
// last-admin check) across processes on this server. Never return tracked entities to a circuit.
public sealed partial class ServiceStore : IDisposable
{
    public event Action? Changed;
    private readonly DbContextOptions<ServiceDbContext> options;
    private readonly Mutex mutex;
    private readonly IDataProtector protector;
    public string Path { get; }

    public ServiceStore(IConfiguration config, IDataProtectionProvider protection, IWebHostEnvironment environment)
    {
        Path = System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(config["ServiceDatabase:Path"]
            ?? "%ProgramData%/DiplomaTry2/service.sqlite"));
        var root = System.IO.Path.GetFullPath(environment.ContentRootPath).TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar;
        if (Path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Служебная БД должна находиться вне каталога приложения.");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        protector = protection.CreateProtector("DiplomaTry2.ServiceDatabase.Connection.v1");
        mutex = new Mutex(false, "Global\\DiplomaService_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.ToUpperInvariant()))));
        options = new DbContextOptionsBuilder<ServiceDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path, Pooling = false }.ToString()).Options;
        try {
            Locked(() => {
                using var db = new ServiceDbContext(options);
                ServiceDatabaseUpgrade.Initialize(db, Path);
                return true;
            });
            Access(u => {
                foreach (var role in new[] { "User", "Admin" })
                    if (!u.Roles.Any(r => r.Name == role)) u.Roles.Add(new ServiceRole { Name = role });
                if (Get(u, "Initialized") is null)
                {
                    Set(u, "Connection", protector.Protect(config.GetConnectionString("DefaultConnection") ?? ""));
                    Set(u, "PrintServer", config["PrintSource:ServerName"] ?? "");
                    Set(u, "Initialized", "1");
                }
                if (Get(u, "LegacyConnection") is null)
                    Set(u, "LegacyConnection", protector.Protect(config.GetConnectionString("DefaultConnection") ?? ""));
                if (Get(u, "DatabaseConnectionListInitialized") is null) {
                    var currentConnection = protector.Unprotect(Get(u, "Connection")!);
                    if (!string.IsNullOrWhiteSpace(currentConnection)) {
                        try { SaveActiveConnection(u, currentConnection, Get(u, "Connection")); }
                        catch (ArgumentException) { /* Keep invalid legacy settings available for correction. */ }
                    }
                    Set(u, "DatabaseConnectionListInitialized", "1");
                }
                if (Get(u, "PrintServerListInitialized") is null) {
                    // Get also checks tracked settings before the initialization transaction is committed.
                    var current = Get(u, "PrintServer") ?? config["PrintSource:ServerName"];
                    if (!string.IsNullOrWhiteSpace(current)) {
                        try { EnsurePrintServer(u, NormalizeHost(current)); }
                        catch (ArgumentException) { /* Preserve an invalid legacy setting for the administrator to correct. */ }
                    }
                    Set(u, "PrintServerListInitialized", "1");
                }
                return true;
            });
        }
        catch { mutex.Dispose(); throw; }
    }

    private T Access<T>(Func<ServiceDbContext, T> action) => Locked(() => {
        using var db = new ServiceDbContext(options);
        using var transaction = db.Database.BeginTransaction();
        var result = action(db);
        db.SaveChanges();
        transaction.Commit();
        return result;
    });

    private T Locked<T>(Func<T> action)
    {
        try { if (!mutex.WaitOne(TimeSpan.FromSeconds(30))) throw new TimeoutException("Служебная БД занята."); }
        catch (AbandonedMutexException) { }
        try { return action(); }
        finally { mutex.ReleaseMutex(); }
    }
    private static string? Get(ServiceDbContext u, string key) => (u.Settings.Local.FirstOrDefault(s => s.Key == key) ?? u.Settings.FirstOrDefault(s => s.Key == key))?.Value;
    private static void Set(ServiceDbContext u, string key, string value)
    {
        var row = u.Settings.Local.FirstOrDefault(s => s.Key == key) ?? u.Settings.FirstOrDefault(s => s.Key == key) ?? u.Settings.Add(new ServiceSetting { Key = key }).Entity;
        row.Value = value;
    }
    public GlobalSnapshot Snapshot() => Access(u => new GlobalSnapshot(protector.Unprotect(Get(u, "Connection")!), Get(u, "PrintServer") ?? ""));
    internal string LegacyConnection() => Access(u => protector.Unprotect(Get(u, "LegacyConnection")!));
    public string? ReadSetting(string key) => Access(u => Get(u, key));
    public bool HasAdmin() => Access(u => u.Accounts.Any(a => a.Role == "Admin"));
    public bool IsAdmin(string sid) => Access(u => u.Accounts.Any(a => a.Sid == sid && a.Role == "Admin"));
    public string Register(string sid, string name) => Access(u => {
        var a = u.Accounts.FirstOrDefault(a => a.Sid == sid) ?? AddAccount(u, sid);
        a.Name = name; return a.Role;
    });
    internal static void DemandAdmin(ServiceDbContext u, string sid)
    {
        if (!u.Accounts.Any(a => a.Sid == sid && a.Role == "Admin"))
            throw new UnauthorizedAccessException("Требуются права администратора приложения.");
    }
    public IReadOnlyList<AccountInfo> Accounts(string actor) => Access(u => {
        DemandAdmin(u, actor);
        return u.Accounts.ToList().Select(a => new AccountInfo(a.Sid, a.Name, a.Role)).OrderBy(a => a.Name).ToArray();
    });
    public void SaveGlobals(string actor, GlobalSnapshot snapshot) {
        Access(u => { DemandAdmin(u, actor); SaveActiveConnection(u, snapshot.ConnectionString);
            if (!string.IsNullOrWhiteSpace(snapshot.PrintServer)) EnsurePrintServer(u, NormalizeHost(snapshot.PrintServer));
            Set(u, "PrintServer", snapshot.PrintServer); return true; });
        NotifyChanged();
    }
    public void SaveConnection(string actor, string connection) {
        Access(u => { DemandAdmin(u, actor); SaveActiveConnection(u, connection); return true; });
        NotifyChanged();
    }
    public void SavePrintServer(string actor, string server) {
        server = NormalizeHost(server);
        Access(u => { DemandAdmin(u, actor); EnsurePrintServer(u, server); Set(u, "PrintServer", server); return true; });
        NotifyChanged();
    }
    private static string NormalizeHost(string host) => Services.PrintSourceSelection.Validate("Windows", host, "Default").ServerName;
    private static ServicePrintServer EnsurePrintServer(ServiceDbContext u, string host) {
        var key = host.ToUpperInvariant();
        return u.PrintServers.FirstOrDefault(s => s.HostKey == key) ?? u.PrintServers.Add(new ServicePrintServer { Host = host, HostKey = key }).Entity;
    }
    public IReadOnlyList<PrintServerInfo> PrintServers() => Access(u => {
        var active = (Get(u, "PrintServer") ?? "").Trim().TrimStart('\\');
        return u.PrintServers.ToList().Select(s => new PrintServerInfo(s.Host, s.Host.Equals(active, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(s => s.IsActive).ThenBy(s => s.Host, StringComparer.OrdinalIgnoreCase).ToArray();
    });
    public void AddPrintServer(string actor, string host) => Access(u => {
        DemandAdmin(u, actor); EnsurePrintServer(u, NormalizeHost(host)); return true;
    });
    public void ActivatePrintServer(string actor, string host) {
        var key = NormalizeHost(host).ToUpperInvariant();
        Access(u => {
            DemandAdmin(u, actor);
            var saved = u.PrintServers.SingleOrDefault(s => s.HostKey == key)
                ?? throw new InvalidOperationException("Сначала добавьте сервер в список.");
            Set(u, "PrintServer", saved.Host); return true;
        });
        NotifyChanged();
    }
    public void RemovePrintServer(string actor, string host) => Access(u => {
        DemandAdmin(u, actor);
        var key = NormalizeHost(host).ToUpperInvariant();
        if ((Get(u, "PrintServer") ?? "").Trim().TrimStart('\\').ToUpperInvariant() == key)
            throw new InvalidOperationException("Активный сервер удалить нельзя. Сначала выберите другой сервер.");
        var server = u.PrintServers.SingleOrDefault(s => s.HostKey == key);
        if (server is not null) u.PrintServers.Remove(server); return true;
    });
    private void NotifyChanged() {
        foreach (var subscriber in Changed?.GetInvocationList() ?? [])
            try { ((Action)subscriber)(); } catch (ObjectDisposedException) { /* A disconnected circuit cannot undo a committed setting. */ }
    }
    public void ChangeAccount(string actor, string sid, string name, string? role) => Access(u => {
        DemandAdmin(u, actor);
        if (role is not (null or "User" or "Admin")) throw new ArgumentException("Недопустимая роль.");
        var a = u.Accounts.FirstOrDefault(a => a.Sid == sid);
        if (a?.Role == "Admin" && role != "Admin" && u.Accounts.Count(a => a.Role == "Admin") == 1)
            throw new InvalidOperationException("Нельзя удалить или понизить права последнего администратора.");
        if (role is null) { if (a is not null) u.Accounts.Remove(a); return true; }
        a ??= AddAccount(u, sid);
        a.Name = name; a.Role = role; return true;
    });
    public bool Claim(string sid, string name, string? bootstrapSid) => Access(u => {
        if (u.Accounts.Any(a => a.Role == "Admin")) return false;
        if (Get(u, "LegacyMigration") != "Complete" && sid != bootstrapSid)
            throw new InvalidOperationException("Перенос ролей ещё не завершён. Восстановите доступ к прежней БД или задайте ServiceDatabase:BootstrapAdminSid в конфигурации сервера.");
        var a = u.Accounts.FirstOrDefault(a => a.Sid == sid) ?? AddAccount(u, sid);
        a.Name = name; a.Role = "Admin"; return true;
    });
    public void ImportAccounts(IEnumerable<AccountInfo> accounts) => Access(u => {
        if (Get(u, "LegacyMigration") == "Complete") return true;
        foreach (var group in accounts.GroupBy(a => a.Sid, StringComparer.Ordinal)) {
            var item = group.First() with { Role = group.Any(a => a.Role == "Admin") ? "Admin" : "User" };
            var a = u.Accounts.FirstOrDefault(a => a.Sid == item.Sid) ?? AddAccount(u, item.Sid);
            a.Name = item.Name;
            if (item.Role == "Admin") a.Role = "Admin";
        }
        Set(u, "LegacyMigration", "Complete"); return true;
    });
    private static ServiceAccount AddAccount(ServiceDbContext db, string sid) => db.Accounts.Add(new ServiceAccount { Sid = sid }).Entity;
    public void Dispose() => mutex.Dispose();
}
