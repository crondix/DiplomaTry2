using System.Diagnostics.Eventing.Reader;
using DiplomaTry2.InterFaces;
using DiplomaTry2.Models;

namespace DiplomaTry2.Services;

public sealed class WindowsPrintEventSource(PrintSourceSelection selection) : IPrintEventSource
{
    public string SourceKey => selection.ServerName.ToLowerInvariant();
    public string LogName => "Microsoft-Windows-PrintService/Operational";

    public Task<PrintEventBatch> ReadBatchAsync(long? afterRecordId, DateTime? anchorTime, int limit)
    {
        var serverName = selection.ServerName;
        return Task.Run(() => ReadBatch(serverName, afterRecordId, anchorTime, limit));
    }

    private PrintEventBatch ReadBatch(string serverName, long? afterRecordId, DateTime? anchorTime, int limit)
    {
        using var session = new EventLogSession(serverName);
        var lost = afterRecordId.HasValue && !AnchorExists(session, afterRecordId.Value, anchorTime);
        var filter = lost || !afterRecordId.HasValue
            ? "*[System/EventID=307]"
            : $"*[System[(EventID=307) and (EventRecordID > {afterRecordId.Value})]]";
        var query = new EventLogQuery(LogName, PathType.LogName, filter) { Session = session };
        using var reader = new EventLogReader(query);
        var events = new List<EventPrint307>();
        EventRecord? record;
        while (events.Count < limit && (record = reader.ReadEvent()) is not null)
        {
            using (record) events.Add(ConvertRecord(record, serverName.ToLowerInvariant()));
        }
        return new PrintEventBatch(events, lost);
    }

    private bool AnchorExists(EventLogSession session, long recordId, DateTime? time)
    {
        var query = new EventLogQuery(LogName, PathType.LogName,
            $"*[System[(EventID=307) and (EventRecordID = {recordId})]]") { Session = session };
        using var reader = new EventLogReader(query);
        using var record = reader.ReadEvent();
        return record is not null && record.TimeCreated == time;
    }

    private EventPrint307 ConvertRecord(EventRecord record, string sourceKey)
    {
        var p = record.Properties;
        if (p.Count < 8 || !record.RecordId.HasValue || !record.TimeCreated.HasValue)
            throw new FormatException($"Event 307 record {record.RecordId} is missing identity or properties.");
        return new EventPrint307(
            p[1].Value?.ToString() ?? "", p[2].Value?.ToString() ?? "",
            p[3].Value?.ToString() ?? "", p[4].Value?.ToString() ?? "",
            Convert.ToInt64(p[6].Value), Convert.ToInt16(p[7].Value),
            p[5].Value?.ToString() ?? "", record.TimeCreated)
        { SourceName = sourceKey, LogName = LogName, RecordId = record.RecordId };
    }
}
