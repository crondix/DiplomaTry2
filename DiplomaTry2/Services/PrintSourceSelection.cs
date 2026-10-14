namespace DiplomaTry2.Services;

public sealed record PrintSourceChoice(string Mode, string ServerName, string Scenario);

// Test mode is circuit scoped; the real server is read from global settings.
public sealed class PrintSourceSelection : IDisposable
{
    private readonly DiplomaTry2.Data.ServiceStore? store;
    private int revision;
    public PrintSourceSelection(PrintSourceSettings defaults, DiplomaTry2.Data.ServiceStore? store = null)
    {
        this.store = store;
        Mode = store is null && defaults.IsFixture ? "Fixture" : "Windows";
        Scenario = new[] { "Default", "Empty", "Error", "MissingModel" }.First(value => value.Equals(defaults.Scenario, StringComparison.OrdinalIgnoreCase));
        serverName = defaults.ServerName.Trim().TrimStart('\\');
        if (store is not null) store.Changed += NotifyChanged;
    }
    public event Action? Changed;
    public string Mode { get; private set; }
    public string Scenario { get; private set; }
    private string serverName;
    public string ServerName => store?.Snapshot().PrintServer ?? serverName;
    public bool IsFixture => Mode.Equals("Fixture", StringComparison.OrdinalIgnoreCase);
    public int Revision => Volatile.Read(ref revision);
    public PrintSourceChoice Current => new(Mode, ServerName, Scenario);

    public static PrintSourceChoice Validate(string mode, string serverName, string scenario)
    {
        if (mode is not ("Fixture" or "Windows"))
            throw new ArgumentException("Выберите источник данных.");
        if (scenario is not ("Default" or "Empty" or "Error" or "MissingModel"))
            throw new ArgumentException("Выберите тестовый набор данных.");
        var server = (serverName ?? "").Trim().TrimStart('\\');
        if ((mode == "Windows" && server.Length == 0) ||
            (server.Length > 0 && (server.Length > 190 || Uri.CheckHostName(server) == UriHostNameType.Unknown)))
            throw new ArgumentException("Укажите имя или IP-адрес сервера печати без пути и номера порта (до 190 символов).");
        return new(mode, server, scenario);
    }

    public void Apply(string mode, string serverName, string scenario)
    {
        var choice = Validate(mode, serverName, scenario);
        if (choice == Current) return;
        Mode = choice.Mode;
        if (store is null) this.serverName = choice.ServerName;
        Scenario = choice.Scenario;
        NotifyChanged();
    }

    public void SetMode(string mode)
    {
        Apply(mode, ServerName, Scenario);
    }

    public void SetScenario(string scenario)
    {
        Apply(Mode, ServerName, scenario);
    }

    private void NotifyChanged()
    {
        Interlocked.Increment(ref revision);
        Changed?.Invoke();
    }

    public void Dispose() { if (store is not null) store.Changed -= NotifyChanged; }
    public PrintSourceSelection Capture(string? server = null) => new(new PrintSourceSettings { Mode = Mode, ServerName = server ?? ServerName, Scenario = Scenario });
}

