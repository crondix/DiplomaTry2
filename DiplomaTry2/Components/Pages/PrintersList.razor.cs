using DiplomaModels;
using DiplomaTry2.Data;
using DiplomaTry2.InterFaces;
using DiplomaTry2.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Components.Pages
{
    public partial class PrintersList : IDisposable
    {
        [Inject]
        IPrinterSource printserver { get; set; } = default!;
        [Inject]
        PrintSourceSelection SourceSelection { get; set; } = default!;
        [Inject] private ILogger<PrintersList> Logger { get; set; } = default!;

        List<NetworkPrinter>? printers { get; set; }
        string message { get; set; } = "";
        bool Loading = false;

        protected override void OnInitialized() => SourceSelection.Changed += SourceChanged;

        private void SourceChanged()
        {
            printers = null;
            message = "";
            _ = InvokeAsync(StateHasChanged);
        }

        public void Dispose() => SourceSelection.Changed -= SourceChanged;

        async Task LoadingData()
        {
            if (Loading) return;
            Loading = true;
            message = "";
            var revision = SourceSelection.Revision;
            try
            {
                var result = await printserver.GetPrintersAsync();
                if (revision == SourceSelection.Revision)
                    printers = result.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Не удалось загрузить список принтеров с сервера печати.");
                if (revision == SourceSelection.Revision)
                    message = $"Не удалось получить принтеры: {ex.Message}";
            }
            finally
            {
                Loading = false;
            }
        }
    }
}
