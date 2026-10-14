using System.IO;
using DiplomaModels;
using DiplomaTry2.InterFaces;
using DiplomaTry2.Models;

namespace DiplomaTry2.Services;

public sealed class FixturePrintSource(PrintSourceSelection selection) : IPrinterSource, IPrintEventSource
{
    private static readonly DateTime SampleTime = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Local);

    public Task<IReadOnlyList<PrinterModel>> GetModelsAsync()
    {
        CheckError();
        IReadOnlyList<PrinterModel> result = IsEmpty ? [] :
        [
            new() { ModelName = "HP LaserJet Test", IsDuplexing = true, IsColor = false, MaxCopyPerPrint = 99 },
            new() { ModelName = "Canon Color Test", IsDuplexing = true, IsColor = true, MaxCopyPerPrint = 50 },
            new() { ModelName = "Kyocera Mono Test", IsDuplexing = false, IsColor = false, MaxCopyPerPrint = 25 }
        ];
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<NetworkPrinter>> GetPrintersAsync()
    {
        CheckError();
        IReadOnlyList<NetworkPrinter> result = IsEmpty ? [] :
        [
            new() { Name = "TEST_HP_01", ShareName = "TEST_HP_01", Comment = "Тестовый принтер", Ip = "192.0.2.10", DriverName = "HP LaserJet Test", IsOnline = true },
            new() { Name = "TEST_CANON_02", ShareName = "TEST_CANON_02", NonIPAddress = "TEST_PORT_02", DriverName = "Canon Color Test", IsOnline = false },
            new() { Name = "TEST_KYOCERA_03", ShareName = "TEST_KYOCERA_03", Ip = "192.0.2.12", DriverName = IsMissingModel ? null : "Kyocera Mono Test", IsOnline = true }
        ];
        return Task.FromResult(result);
    }

    public string SourceKey => "fixture";
    public string LogName => "Microsoft-Windows-PrintService/Operational";

    public Task<PrintEventBatch> ReadBatchAsync(long? afterRecordId, DateTime? anchorTime, int limit)
    {
        CheckError();
        IReadOnlyList<EventPrint307> all = IsEmpty ? [] :
        [
            Event(1001, "report.pdf", "TEST_HP_01", SampleTime),
            Event(1002, "chart.pdf", "TEST_CANON_02", SampleTime),
            Event(1002, "chart.pdf", "TEST_CANON_02", SampleTime), // повторная запись источника
            Event(1003, "unknown.pdf", "TEST_UNKNOWN_99", SampleTime.AddMinutes(1)),
            Event(1004, "memo.pdf", "TEST_KYOCERA_03", SampleTime.AddMinutes(2))
        ];
        var cursorLost = afterRecordId.HasValue && !all.Any(e => e.RecordId == afterRecordId && e.DateTime == anchorTime);
        var result = all.Where(e => cursorLost || !afterRecordId.HasValue || e.RecordId > afterRecordId)
            .OrderBy(e => e.RecordId).Take(limit).ToArray();
        return Task.FromResult(new PrintEventBatch(result, cursorLost));
    }

    private static EventPrint307 Event(long recordId, string document, string printer, DateTime time) =>
        new(document, "test.user", "TEST_PC", printer, 2048, 2, "TEST_PORT", time)
        { SourceName = "fixture", RecordId = recordId };

    private bool IsEmpty => selection.Scenario.Equals("Empty", StringComparison.OrdinalIgnoreCase);
    private bool IsMissingModel => selection.Scenario.Equals("MissingModel", StringComparison.OrdinalIgnoreCase);

    private void CheckError()
    {
        if (selection.Scenario.Equals("Error", StringComparison.OrdinalIgnoreCase))
            throw new IOException("Тестовая ошибка источника печати (PrintSource:Scenario=Error).");
    }
}
