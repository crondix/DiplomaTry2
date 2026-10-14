namespace DiplomaTry2.Data;

public sealed class PrintEventImportCursor
{
    public string SourceKey { get; set; } = "";
    public string LogName { get; set; } = "";
    public long RecordId { get; set; }
    public DateTime? EventTime { get; set; }
}
