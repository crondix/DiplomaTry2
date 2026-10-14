using System.Text.Json;
using Microsoft.JSInterop;

namespace DiplomaTry2.Services;

// One snapshot keeps the server, mode and scenario consistent after a full page reload.
public sealed class PrintSourcePreferences(IJSRuntime js, PrintSourceSelection selection, PrintSourceSettings defaults,
    ApplicationSettingsService settings)
{
    private const string StorageKey = "diploma:printSource:v1";

    public async Task LoadAsync()
    {
        try { await settings.RequireAdminAsync(); }
        catch (UnauthorizedAccessException) { return; }
        var json = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
        PrintSourceChoice? choice = null;
        if (json is not null)
        {
            try { choice = JsonSerializer.Deserialize<PrintSourceChoice>(json); }
            catch (JsonException) { }
        }
        else
        {
            // Preserve choices made with the previous source switcher.
            var mode = await js.InvokeAsync<string?>("sessionStorage.getItem", "diploma:printSourceMode");
            var scenario = await js.InvokeAsync<string?>("sessionStorage.getItem", "diploma:printSourceScenario");
            choice = new(mode ?? selection.Mode, defaults.ServerName, scenario ?? selection.Scenario);
        }
        if (choice is null) return;
        try { selection.Apply(choice.Mode, selection.ServerName, choice.Scenario); }
        catch (ArgumentException) { /* Invalid browser data must not break the circuit. */ }
    }

    public async Task SaveAsync(string mode, string serverName, string scenario)
    {
        await settings.RequireAdminAsync();
        var choice = PrintSourceSelection.Validate(mode, selection.ServerName, scenario);
        await js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, JsonSerializer.Serialize(choice));
        selection.Apply(choice.Mode, choice.ServerName, choice.Scenario);
    }
}
