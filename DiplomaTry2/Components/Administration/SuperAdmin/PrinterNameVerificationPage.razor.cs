using DiplomaModels;
using DiplomaTry2.Components.Tables;
using DiplomaTry2.Services;

namespace DiplomaTry2.Components.Administration.SuperAdmin;

public partial class PrinterNameVerificationPage
{
    private static readonly GridColumn[] ModelColumns = [
        new("Модель", "Name", WidthGrow: 2), new("Цветная", "Color"),
        new("Двусторонняя печать", "Duplex"), new("Максимум копий", "Copies")
    ];
    public sealed record ModelRowView(int Id, string Name, string Color, string Duplex, short? Copies);
    private static string YesNo(bool? value) => value.HasValue ? (value.Value ? "Да" : "Нет") : "Не указано";
    private static ModelRowView ModelRow(PrinterModel model, int index) =>
        new(model.Id, model.ModelName, YesNo(model.IsColor), YesNo(model.IsDuplexing), model.MaxCopyPerPrint);
    private List<PrinterModel> models = [];
    private PrinterModelService catalog = default!;
    private PrinterModelDraft? draft;
    private PrinterModel? deleteModel;
    private int linkedPrinters;
    private string? error, message;
    private bool busy, showDrivers, reloadPending, modelsReady;
    private Dictionary<string, string> originalDrivers = [];
    private List<string> available = [], chosen = [];
    private IEnumerable<string> leftSelection = [], rightSelection = [];
    private string renamedName = "";

    protected override async Task OnInitializedAsync()
    {
        Selection.Changed += SourceChanged;
        await RunAsync(ReloadAsync);
    }
    private async Task ReloadAsync()
    {
        modelsReady = false;
        catalog = ModelService.Capture();
        models = await catalog.ListAsync();
        modelsReady = true;
    }
    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true; error = null; message = null;
        try { await action(); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Операция со справочником моделей не выполнена.");
            error = ex is Microsoft.EntityFrameworkCore.DbUpdateException
                ? "Не удалось сохранить изменения. Возможно, название уже добавлено другим пользователем. Обновите список и повторите операцию."
                : ex.Message;
        }
        finally
        {
            while (reloadPending)
            {
                reloadPending = false;
                CloseDrivers(); draft = null; deleteModel = null; message = null;
                try { await ReloadAsync(); }
                catch (Exception ex) { error = $"Не удалось обновить справочник: {ex.Message}"; }
            }
            busy = false;
        }
    }
    private void AddModel()
    {
        deleteModel = null; error = null; message = null; draft = new();
    }
    private void EditModel(int id)
    {
        var model = models.Single(m => m.Id == id);
        deleteModel = null; error = null; message = null;
        draft = new() { Id = id, Name = model.ModelName, IsColor = model.IsColor,
            IsDuplexing = model.IsDuplexing, MaxCopyPerPrint = model.MaxCopyPerPrint };
    }
    private Task SaveModelAsync() => RunAsync(async () =>
    {
        await catalog.SaveAsync(draft!); draft = null;
        await ReloadAsync(); message = "Модель сохранена.";
    });
    private Task ConfirmDeleteAsync(int id) => RunAsync(async () =>
    {
        var model = models.Single(m => m.Id == id);
        var count = await catalog.CountPrintersAsync(id);
        draft = null; linkedPrinters = count; deleteModel = model;
    });
    private Task DeleteModelAsync() => RunAsync(async () =>
    {
        await catalog.DeleteAsync(deleteModel!.Id); deleteModel = null;
        await ReloadAsync(); message = "Модель удалена. Принтеры сохранены.";
    });
    private Task FetchDriversAsync() => RunAsync(async () =>
    {
        var revision = Selection.Revision;
        var drivers = await PrinterSource.GetModelsAsync();
        if (revision != Selection.Revision) return;
        await ReloadAsync();
        var keys = models.Select(m => PrinterModelService.NameKey(m.ModelName)).ToHashSet();
        available = drivers.Where(m => !string.IsNullOrWhiteSpace(m.ModelName)).Select(m => m.ModelName.Trim())
            .DistinctBy(PrinterModelService.NameKey).Where(n => !keys.Contains(PrinterModelService.NameKey(n)))
            .Order(StringComparer.OrdinalIgnoreCase).ToList();
        chosen = []; leftSelection = []; rightSelection = []; originalDrivers = []; showDrivers = true;
        message = available.Count == 0 ? "Новых названий драйверов нет." : null;
    });
    private void MoveRight()
    {
        if (leftSelection.Any(n => chosen.Any(c => PrinterModelService.NameKey(c) == PrinterModelService.NameKey(n))))
        { error = "Такое название уже выбрано для создания модели."; return; }
        foreach (var name in leftSelection) originalDrivers[name] = name;
        chosen = chosen.Concat(leftSelection).Distinct().Order(StringComparer.OrdinalIgnoreCase).ToList();
        available = available.Except(leftSelection).ToList(); leftSelection = [];
    }
    private void MoveLeft()
    {
        available = available.Concat(rightSelection.Select(n => originalDrivers[n])).Distinct().Order(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var name in rightSelection) originalDrivers.Remove(name);
        chosen = chosen.Except(rightSelection).ToList(); rightSelection = [];
    }
    private void RightSelectionChanged(IEnumerable<string> values)
    {
        rightSelection = values.ToArray();
        renamedName = rightSelection.Count() == 1 ? rightSelection.Single() : "";
    }
    private void RenameSelected()
    {
        if (string.IsNullOrWhiteSpace(renamedName)) { error = "Укажите название модели."; return; }
        var old = rightSelection.Single(); var name = renamedName.Trim();
        var key = PrinterModelService.NameKey(name);
        if (models.Any(m => PrinterModelService.NameKey(m.ModelName) == key) ||
            chosen.Any(n => n != old && PrinterModelService.NameKey(n) == key))
        { error = "Модель с таким названием уже существует или выбрана."; return; }
        chosen = chosen.Select(n => n == old ? name : n).Order(StringComparer.OrdinalIgnoreCase).ToList();
        var original = originalDrivers[old];
        originalDrivers.Remove(old); originalDrivers[name] = original;
        rightSelection = [name]; error = null;
    }
    private Task SaveSelectedAsync() => RunAsync(async () =>
    {
        var added = await catalog.AddSelectedAsync(chosen);
        await ReloadAsync(); CloseDrivers();
        message = $"Моделей добавлено: {added}. Характеристики можно заполнить через редактирование.";
    });
    private void CloseDrivers()
    {
        showDrivers = false; available = []; chosen = []; leftSelection = []; rightSelection = []; originalDrivers = [];
    }
    private void SourceChanged() => _ = InvokeAsync(async () =>
    {
        CloseDrivers(); draft = null; deleteModel = null; models = []; modelsReady = false;
        if (busy) reloadPending = true;
        else await RunAsync(ReloadAsync);
        StateHasChanged();
    });
    public void Dispose() => Selection.Changed -= SourceChanged;
}
