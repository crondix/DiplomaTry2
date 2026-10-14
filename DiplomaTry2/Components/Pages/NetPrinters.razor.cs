using DiplomaModels;
using DiplomaTry2.Data;
using DiplomaTry2.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Components.Pages;

public partial class NetPrinters : IDisposable
{
    [Inject] private NetPrintersService Importer { get; set; } = default!;
    [Inject] private IDbContextFactory<ApplicationDbContext> ContextFactory { get; set; } = default!;
    [Inject] private PrintSourceSelection SourceSelection { get; set; } = default!;
    [Inject] private ILogger<NetPrinters> Logger { get; set; } = default!;
    [Inject] private PrinterModelService ModelService { get; set; } = default!;
    private PrinterModelService catalog = default!;
    private NetworkPrinter? assignPrinter;
    private int? assignedModelId;
    private List<PrinterModel> modelChoices = [];

    private List<NetworkPrinter> savedPrinters = [];
    private string? statusMessage;
    private string? errorMessage;
    private bool busy;
    private bool loadingSaved;

    protected override async Task OnInitializedAsync()
    {
        SourceSelection.Changed += SourceChanged;
        await LoadSavedAsync();
    }

    private async Task LoadSavedAsync()
    {
        loadingSaved = true;
        var revision = SourceSelection.Revision;
        try
        {
            var factory = ContextFactory is RuntimeDatabase runtime ? runtime.Capture() : ContextFactory;
            catalog = ModelService.Capture(factory);
            await using var db = await factory.CreateDbContextAsync();
            var loaded = await db.NetworkPrinters.AsNoTracking()
                .Include(p => p.PrinterModel)
                .OrderBy(p => p.SourceKey).ThenBy(p => p.Name).ToListAsync();
            if (revision != SourceSelection.Revision) return;
            savedPrinters = loaded;
            errorMessage = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Не удалось загрузить сохранённые принтеры из основной БД.");
            errorMessage = "Основная БД недоступна, не настроена или её схема несовместима. Администратор может проверить подключение на странице /settings.";
        }
        finally
        {
            loadingSaved = false;
            if (revision != SourceSelection.Revision) await LoadSavedAsync();
        }
    }

    private async Task ImportAsync()
    {
        if (busy || loadingSaved) return;
        busy = true;
        statusMessage = null;
        errorMessage = null;
        try
        {
            var result = await Importer.ImportAsync();
            await LoadSavedAsync();
            if (errorMessage is null)
                statusMessage = $"Принтеров добавлено: {result.PrintersAdded}, обновлено: {result.PrintersUpdated}; без назначенной модели: {result.WithoutModel}.";
        }
        catch (PrinterSourceException ex)
        {
            errorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            errorMessage = $"Не удалось сохранить принтеры: {ex.Message}";
        }
        finally
        {
            busy = false;
        }
    }

    private void SourceChanged() => _ = InvokeAsync(async () =>
    {
        assignPrinter = null;
        savedPrinters = [];
        statusMessage = null;
        errorMessage = null;
        if (!busy && !loadingSaved) await LoadSavedAsync();
        StateHasChanged();
    });

    private async Task EditAssignmentAsync(int id)
    {
        if (busy || loadingSaved) return;
        busy = true; errorMessage = null;
        var revision = SourceSelection.Revision;
        try
        {
            modelChoices = await catalog.ListAsync();
            if (revision != SourceSelection.Revision) return;
            assignPrinter = savedPrinters.Single(p => p.Id == id);
            assignedModelId = assignPrinter.PrinterModelId;
        }
        catch (Exception ex) { errorMessage = $"Не удалось загрузить модели: {ex.Message}"; }
        finally
        {
            busy = false;
            if (revision != SourceSelection.Revision) await LoadSavedAsync();
        }
    }

    private async Task SaveAssignmentAsync()
    {
        if (busy || assignPrinter is null) return;
        busy = true; errorMessage = null;
        try
        {
            // Keep the database that supplied the selected row; require administrator rights before writing.
            var printerId = assignPrinter.Id;
            var modelId = assignedModelId;
            await catalog.AssignAsync(printerId, modelId);
            assignPrinter = null;
            await LoadSavedAsync();
            if (errorMessage is null) statusMessage = "Модель принтера сохранена.";
        }
        catch (Exception ex) { errorMessage = $"Не удалось назначить модель: {ex.Message}"; }
        finally { busy = false; }
    }


    public void Dispose() => SourceSelection.Changed -= SourceChanged;
}
