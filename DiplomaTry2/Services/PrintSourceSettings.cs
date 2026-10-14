namespace DiplomaTry2.Services;

public sealed class PrintSourceSettings
{
    public string Mode { get; init; } = "Fixture";
    public string ServerName { get; init; } = "";
    public string Scenario { get; init; } = "Default";

    public bool IsFixture => Mode.Equals("Fixture", StringComparison.OrdinalIgnoreCase);

    public static PrintSourceSettings FromConfiguration(IConfiguration configuration)
    {
        var settings = configuration.GetSection("PrintSource").Get<PrintSourceSettings>() ?? new();
        if (!settings.IsFixture && !settings.Mode.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PrintSource:Mode must be Fixture or Windows.");
        if (!new[] { "Default", "Empty", "Error", "MissingModel" }.Contains(settings.Scenario, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("PrintSource:Scenario must be Default, Empty, Error or MissingModel.");
        return settings;
    }
}
