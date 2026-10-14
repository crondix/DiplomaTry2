using DiplomaTry2.Models;

namespace DiplomaTry2.InterFaces;

public interface IPrintEventSource
{
    string SourceKey { get; }
    string LogName { get; }
    Task<PrintEventBatch> ReadBatchAsync(long? afterRecordId, DateTime? anchorTime, int limit);
}

public sealed record PrintEventBatch(IReadOnlyList<EventPrint307> Events, bool CursorLost);
