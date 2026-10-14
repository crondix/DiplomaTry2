namespace DiplomaTry2.Services;

public sealed class ApplicationLogService(ApplicationLogStore store, ApplicationSettingsService settings)
{
    public async Task<ApplicationLogSnapshot> ReadAsync()
    {
        // Recheck the current assignment on every refresh, including an already open circuit.
        await settings.RequireAdminAsync();
        return store.Snapshot();
    }
}
