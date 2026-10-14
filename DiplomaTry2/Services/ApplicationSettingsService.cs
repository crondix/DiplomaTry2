using System.Security.Principal;
using DiplomaTry2.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.SqlClient;

namespace DiplomaTry2.Services;

public sealed record ConnectionForm(string Server, string Database, bool IntegratedSecurity, string Login, string? Password,
    bool TrustServerCertificate = false);

public sealed class ApplicationSettingsService(ServiceStore store, AuthenticationStateProvider authentication, PrintServerProbe probe,
    ILogger<ApplicationSettingsService>? logger = null, IMainDatabaseValidator? databaseValidator = null)
{
    public async Task<string> RequireAdminAsync()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        var account = FirstRunAdminSetup.WindowsAccount(user);
        if (account is null || !store.IsAdmin(account.Value.Sid)) throw new UnauthorizedAccessException("Требуются права администратора приложения.");
        return account.Value.Sid;
    }
    public GlobalSnapshot Read() => store.Snapshot();
    public IReadOnlyList<DatabaseConnectionInfo> DatabaseConnections() => store.DatabaseConnections();

    public async Task<int> SaveDatabaseConnectionAsync(ConnectionForm form, int? id = null)
    {
        var actor = await RequireAdminAsync();
        var previous = id.HasValue ? store.ReadDatabaseConnection(actor, id.Value).ConnectionString : "";
        var connection = BuildConnectionString(form, previous);
        await ValidateDatabaseConnectionAsync(connection);
        var savedId = store.SaveDatabaseConnection(actor, id, connection);
        logger?.LogInformation("Подключение {Id} к БД {Server}/{Database} сохранено в список. Администратор: {Actor}.", savedId, form.Server, form.Database, actor);
        return savedId;
    }

    public async Task CheckDatabaseConnectionAsync(ConnectionForm form, int? id = null)
    {
        var actor = await RequireAdminAsync();
        var previous = id.HasValue ? store.ReadDatabaseConnection(actor, id.Value).ConnectionString : "";
        await ValidateDatabaseConnectionAsync(BuildConnectionString(form, previous));
    }

    public async Task CheckDatabaseConnectionAsync(int id)
    {
        var actor = await RequireAdminAsync();
        await ValidateDatabaseConnectionAsync(store.ReadDatabaseConnection(actor, id).ConnectionString);
    }

    public async Task ActivateDatabaseConnectionAsync(int id)
    {
        var actor = await RequireAdminAsync();
        var selected = store.ReadDatabaseConnection(actor, id);
        await ValidateDatabaseConnectionAsync(selected.ConnectionString);
        store.ActivateDatabaseConnection(actor, id, selected.Version);
        logger?.LogInformation("Активное подключение к основной БД изменено на {Id}. Администратор: {Actor}.", id, actor);
    }

    public async Task RemoveDatabaseConnectionAsync(int id)
    {
        var actor = await RequireAdminAsync();
        store.RemoveDatabaseConnection(actor, id);
        logger?.LogInformation("Подключение {Id} удалено из списка. Администратор: {Actor}.", id, actor);
    }

    private async Task ValidateDatabaseConnectionAsync(string connection)
    {
        try { await (databaseValidator ?? new MainDatabaseValidator()).ValidateAsync(connection); }
        catch (Exception ex) {
            var cs = new SqlConnectionStringBuilder(connection);
            logger?.LogError(ex, "Проверка основной БД {Server}/{Database} не пройдена.", cs.DataSource, cs.InitialCatalog);
            throw new InvalidOperationException("Подключение или схема БД не прошли проверку. Проверьте доступ и сертификат сервера. Рабочие настройки сохранены.");
        }
    }
    public IReadOnlyList<PrintServerInfo> PrintServers() => store.PrintServers();
    public async Task AddPrintServerAsync(string host) {
        var actor = await RequireAdminAsync();
        store.AddPrintServer(actor, host);
        logger?.LogInformation("Сервер печати {Host} добавлен в список. Администратор: {Actor}.", host, actor);
    }
    public async Task ActivatePrintServerAsync(string host) {
        var actor = await RequireAdminAsync();
        store.ActivatePrintServer(actor, host);
        logger?.LogInformation("Активный сервер печати изменён на {Host}. Администратор: {Actor}.", host, actor);
    }
    public async Task RemovePrintServerAsync(string host) {
        var actor = await RequireAdminAsync();
        store.RemovePrintServer(actor, host);
        logger?.LogInformation("Сервер печати {Host} удалён из списка. Администратор: {Actor}.", host, actor);
    }
    public async Task<PrintServerProbeResult> CheckPrintServerAsync(string host, CancellationToken token = default) {
        await RequireAdminAsync();
        var result = await probe.CheckAsync(host, token);
        logger?.LogInformation("Проверка сервера печати {Host}: DNS — {Dns}; ping — {Ping}; RPC — {Rpc}.", result.Host, result.Dns.Detail, result.Ping.Detail, result.Rpc.Detail);
        return result;
    }
    public async Task<IReadOnlyList<AccountInfo>> AccountsAsync() => store.Accounts(await RequireAdminAsync());
    public async Task ChangeAccountAsync(string accountName, string? role)
    {
        var actor = await RequireAdminAsync();
        // Windows resolves the account; no passwords, account creation or group management.
        var sid = (SecurityIdentifier)new NTAccount(accountName.Trim()).Translate(typeof(SecurityIdentifier));
        var name = ((NTAccount)sid.Translate(typeof(NTAccount))).Value;
        store.ChangeAccount(actor, sid.Value, name, role);
        logger?.LogInformation("Доступ учётной записи {Account} изменён: {Role}. Администратор: {Actor}.", name, role ?? "удалён", actor);
    }
    public async Task ChangeExistingAsync(AccountInfo account, string? role) {
        var actor = await RequireAdminAsync();
        store.ChangeAccount(actor, account.Sid, account.Name, role);
        logger?.LogInformation("Доступ учётной записи {Account} изменён: {Role}. Администратор: {Actor}.", account.Name, role ?? "удалён", actor);
    }

    private string Build(ConnectionForm form) => BuildConnectionString(form, store.Snapshot().ConnectionString);

    internal static string BuildConnectionString(ConnectionForm form, string previousConnection)
    {
        if (string.IsNullOrWhiteSpace(form.Server) || string.IsNullOrWhiteSpace(form.Database))
            throw new ArgumentException("Укажите сервер и имя основной БД.");
        var prior = string.IsNullOrWhiteSpace(previousConnection) ? new SqlConnectionStringBuilder() : new SqlConnectionStringBuilder(previousConnection);
        var sameIdentity = prior.DataSource == form.Server.Trim() && prior.InitialCatalog == form.Database.Trim() && prior.UserID == form.Login.Trim();
        var cs = new SqlConnectionStringBuilder {
            DataSource = form.Server.Trim(), InitialCatalog = form.Database.Trim(), IntegratedSecurity = form.IntegratedSecurity,
            ConnectTimeout = 5, Encrypt = true, TrustServerCertificate = form.TrustServerCertificate, PersistSecurityInfo = false
        };
        if (!form.IntegratedSecurity) {
            cs.UserID = form.Login.Trim();
            cs.Password = form.Password ?? (sameIdentity ? prior.Password : "");
            if (string.IsNullOrWhiteSpace(cs.UserID) || string.IsNullOrEmpty(cs.Password)) throw new ArgumentException("Укажите SQL-логин и пароль нового подключения.");
        }
        return cs.ConnectionString;
    }
    public async Task CheckAsync(ConnectionForm form)
    {
        await RequireAdminAsync();
        var connection = Build(form);
        try { await (databaseValidator ?? new MainDatabaseValidator()).ValidateAsync(connection); }
        catch (Exception ex) {
            logger?.LogError(ex, "Не удалось проверить подключение к основной БД {Server}/{Database}.", form.Server, form.Database);
            throw new InvalidOperationException("Не удалось проверить основную БД: проверьте доступ, сертификат сервера и совместимость схемы. Рабочие настройки сохранены.");
        }
        logger?.LogInformation("Проверка подключения к основной БД {Server}/{Database} завершена успешно.", form.Server, form.Database);
    }
    public async Task SaveConnectionAsync(ConnectionForm form)
    {
        var actor = await RequireAdminAsync();
        var connection = Build(form);
        try { await (databaseValidator ?? new MainDatabaseValidator()).ValidateAsync(connection); }
        catch (Exception ex) {
            logger?.LogError(ex, "Подключение к основной БД {Server}/{Database} не прошло проверку перед сохранением.", form.Server, form.Database);
            throw new InvalidOperationException("Подключение или схема БД не прошли проверку. Рабочие настройки сохранены.");
        }
        store.SaveConnection(actor, connection);
        logger?.LogInformation("Подключение к основной БД {Server}/{Database} сохранено. Администратор: {Actor}.", form.Server, form.Database, actor);
    }
    public async Task SavePrintServerAsync(string server)
    {
        var actor = await RequireAdminAsync();
        var choice = PrintSourceSelection.Validate("Windows", server, "Default");
        store.SavePrintServer(actor, choice.ServerName);
        logger?.LogInformation("Сервер печати изменён на {Host}. Администратор: {Actor}.", choice.ServerName, actor);
    }
}
