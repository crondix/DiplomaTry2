using DiplomaModels;
using DiplomaTry2.Data;
using DiplomaTry2.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Components.Pages;

public partial class EventsSuccessfullyPrintingView : IDisposable
{
    [Inject] private EventLogProcessingService Importer { get; set; } = default!;
    [Inject] private IDbContextFactory<ApplicationDbContext> ContextFactory { get; set; } = default!;
    [Inject] private PrintSourceSelection SourceSelection { get; set; } = default!;
    [Inject] private ILogger<EventsSuccessfullyPrintingView> Logger { get; set; } = default!;

    private List<EventSuccessfulPrinting> EventsSuccessfulPrinting = [];
    private string? AdditionErrorMessage;
    private string? ImportSummary;
    private string? NavigationNotice;
    private bool Loading;
    private bool Adding;
    private int _currentItem;

    private void ShowNavigationNotice(string message) => NavigationNotice = message;

    protected override async Task OnInitializedAsync()
    {
        SourceSelection.Changed += SourceChanged;
        await LoadData();
    }

    private void SourceChanged()
    {
        EventsSuccessfulPrinting = [];
        ImportSummary = null;
        AdditionErrorMessage = null;
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnProgressChanged(int percent, int currentItem, int totalItem)
    {
        _ = InvokeAsync(() =>
        {
            _currentItem = currentItem;
            StateHasChanged();
        });
    }

    private async Task Actualization()
    {
        if (Adding || Loading) return;
        Adding = true;
        ImportSummary = null;
        AdditionErrorMessage = null;
        _currentItem = 0;
        Importer.AddProgressChanged += OnProgressChanged;
        try
        {
            var result = await Importer.DbActualizationAsync();
            var summary = $"Прочитано: {result.Read}, добавлено: {result.Added}, пропущено: {result.Skipped}.";
            if (result.Error is null)
                ImportSummary = summary;
            else
                AdditionErrorMessage = $"Импорт завершился с ошибкой: {result.Error} {summary}";
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Не удалось начать импорт событий печати.");
            AdditionErrorMessage = $"Не удалось импортировать события: {ex.Message}";
        }
        finally
        {
            Importer.AddProgressChanged -= OnProgressChanged;
            Adding = false;
        }
    }

    private async Task LoadData()
    {
        if (Loading || Adding) return;
        await RefreshDataAsync();
    }

    private async Task RefreshDataAsync()
    {
        Loading = true;
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            EventsSuccessfulPrinting = await db.EventsSuccessfulPrinting.AsNoTracking()
                .Include(e => e.SenderDevice)
                .Include(e => e.TargetPrinter).ThenInclude(p => p.NetworkPrinter)
                .Include(e => e.Sender)
                .Include(e => e.SentPrintingFile).ThenInclude(f => f.Name)
                .OrderByDescending(e => e.DateTime).Take(50).ToListAsync();
            if (!Adding) AdditionErrorMessage = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Не удалось загрузить события печати из основной БД.");
            AdditionErrorMessage = "Основная БД недоступна, не настроена или её схема несовместима. Администратор может проверить подключение на странице /settings.";
        }
        finally
        {
            Loading = false;
        }
    }

    public void Dispose()
    {
        SourceSelection.Changed -= SourceChanged;
        Importer.AddProgressChanged -= OnProgressChanged;
    }
}
