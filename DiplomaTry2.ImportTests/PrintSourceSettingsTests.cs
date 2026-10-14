using System.Text.Json;
using DiplomaTry2.Services;
using Microsoft.JSInterop;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class PrintSourceSettingsTests : IDisposable
{
    private readonly StoreFixture fixture = CreateFixture();
    private static StoreFixture CreateFixture() { var f = new StoreFixture(); f.MakeAdmin(); return f; }
    public void Dispose() => fixture.Dispose();
    private static readonly PrintSourceSettings Defaults = new() { ServerName = "vm-print" };

    [Fact]
    public void Apply_changes_all_fields_once_and_updates_event_source_identity()
    {
        var selection = new PrintSourceSelection(Defaults);
        var events = new WindowsPrintEventSource(selection);
        var notifications = 0;
        selection.Changed += () =>
        {
            notifications++;
            Assert.Equal(new PrintSourceChoice("Windows", "PRINT-02", "Empty"), selection.Current);
        };
        selection.Apply("Windows", @" \\PRINT-02 ", "Empty");
        Assert.Equal("print-02", events.SourceKey);
        Assert.Equal(1, selection.Revision);
        Assert.Equal(1, notifications);
        selection.Apply("Windows", "PRINT-02", "Empty");
        Assert.Equal(1, selection.Revision);
    }

    [Theory]
    [InlineData("Windows", "", "Default")]
    [InlineData("Windows", "server/queue", "Default")]
    [InlineData("Windows", "https://server", "Default")]
    [InlineData("Windows", "server:123", "Default")]
    [InlineData("unknown", "server", "Default")]
    [InlineData("Fixture", "server", "unknown")]
    public void Invalid_input_keeps_previous_choice_and_revision(string mode, string server, string scenario)
    {
        var selection = new PrintSourceSelection(Defaults);
        var before = selection.Current;
        Assert.Throws<ArgumentException>(() => selection.Apply(mode, server, scenario));
        Assert.Equal(before, selection.Current);
        Assert.Equal(0, selection.Revision);
    }

    [Fact]
    public async Task Saved_choice_is_restored_into_new_scope_without_changing_other_users()
    {
        var storage = new BrowserStorage();
        var selection = new PrintSourceSelection(Defaults);
        await new PrintSourcePreferences(storage, selection, Defaults, fixture.Settings()).SaveAsync("Windows", "192.168.1.15", "Error");
        var afterReload = new PrintSourceSelection(Defaults);
        await new PrintSourcePreferences(storage, afterReload, Defaults, fixture.Settings()).LoadAsync();
        Assert.Equal(selection.Current, afterReload.Current);
        var otherUser = new PrintSourceSelection(Defaults);
        await new PrintSourcePreferences(new BrowserStorage(), otherUser, Defaults, fixture.Settings()).LoadAsync();
        Assert.Equal("vm-print", otherUser.ServerName);
        Assert.True(otherUser.IsFixture);
        Assert.Equal("vm-print", Defaults.ServerName);
    }

    [Fact]
    public async Task Failed_storage_write_does_not_apply_unsaved_choice()
    {
        var selection = new PrintSourceSelection(Defaults);
        var storage = new BrowserStorage { FailWrites = true };
        var preferences = new PrintSourcePreferences(storage, selection, Defaults, fixture.Settings());
        await Assert.ThrowsAsync<JSException>(() => preferences.SaveAsync("Windows", "print-02", "Default"));
        Assert.True(selection.IsFixture);
        Assert.Equal("vm-print", selection.ServerName);
        Assert.Equal(0, selection.Revision);
    }

    [Theory]
    [InlineData("{invalid")]
    [InlineData("null")]
    [InlineData("{}")]
    
    public async Task Corrupt_browser_data_keeps_defaults(string json)
    {
        var storage = new BrowserStorage();
        storage.Values["diploma:printSource:v1"] = json;
        var selection = new PrintSourceSelection(Defaults);
        await new PrintSourcePreferences(storage, selection, Defaults, fixture.Settings()).LoadAsync();
        Assert.True(selection.IsFixture);
        Assert.Equal("vm-print", selection.ServerName);
    }

    [Fact]
    public async Task Legacy_selection_is_loaded_and_migrated_on_save()
    {
        var storage = new BrowserStorage();
        storage.Values["diploma:printSourceMode"] = "Windows";
        storage.Values["diploma:printSourceScenario"] = "MissingModel";
        var selection = new PrintSourceSelection(Defaults);
        var preferences = new PrintSourcePreferences(storage, selection, Defaults, fixture.Settings());
        await preferences.LoadAsync();
        Assert.Equal(new PrintSourceChoice("Windows", "vm-print", "MissingModel"), selection.Current);
        await preferences.SaveAsync("Fixture", "print-03", "Empty");
        Assert.Equal(selection.Current, JsonSerializer.Deserialize<PrintSourceChoice>(storage.Values["diploma:printSource:v1"]));
    }

    private sealed class BrowserStorage : IJSRuntime
    {
        public Dictionary<string, string> Values { get; } = [];
        public bool FailWrites { get; init; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            if (identifier == "sessionStorage.getItem")
                return ValueTask.FromResult((TValue)(object?)Values.GetValueOrDefault(key)!);
            if (identifier == "sessionStorage.setItem")
            {
                if (FailWrites) throw new JSException("Storage unavailable");
                Values[key] = (string)args[1]!;
                return ValueTask.FromResult(default(TValue)!);
            }
            throw new InvalidOperationException(identifier);
        }
    }
}

