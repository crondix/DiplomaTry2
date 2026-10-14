using DiplomaTry2.Data;
using DiplomaTry2.InterFaces;
using DiplomaTry2.Models;
using DiplomaTry2.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class PrintImportTests
{
    private const string Log = "Microsoft-Windows-PrintService/Operational";
    private static readonly DateTime Time = new(2026, 1, 15, 10, 30, 0);

    [Fact]
    public async Task Duplicate_same_time_order_and_unknown_printer()
    {
        var test = Create();
        var (factory, source) = test;
        try
        {
            source.Events = [Event(source.SourceKey, 3), Event(source.SourceKey, 2),
                Event(source.SourceKey, 1), Event(source.SourceKey, 2)];
            var importer = new EventLogProcessingService(source, factory);
            Assert.Equal(new PrintImportResult(4, 3, 1), await importer.DbActualizationAsync());
            Assert.Equal(new PrintImportResult(0, 0, 0), await importer.DbActualizationAsync());
            source.Events = [.. source.Events, Event(source.SourceKey, 4)];
            Assert.Equal(new PrintImportResult(1, 1, 0), await importer.DbActualizationAsync());
            await using var db = factory.CreateDbContext();
            Assert.Equal(4, await db.EventsSuccessfulPrinting.CountAsync(e => e.SourceKey == source.SourceKey));
            Assert.True(await db.EventsSuccessfulPrinting.Where(e => e.SourceKey == source.SourceKey)
                .AllAsync(e => e.DateTime == Time));
            Assert.True(await db.EventsSuccessfulPrinting.Where(e => e.SourceKey == source.SourceKey)
                .Include(e => e.TargetPrinter).AllAsync(e => e.TargetPrinter!.NetworkPrinterId == null));
        }
        finally { await Cleanup(factory, source.SourceKey); }
    }

    [Fact]
    public async Task Failure_after_committed_page_resumes_at_next_record()
    {
        var test = Create();
        var (factory, source) = test;
        try
        {
            source.Events = Enumerable.Range(1, 501).Select(i => Event(source.SourceKey, i)).ToArray();
            source.FailOnCall = 2;
            var importer = new EventLogProcessingService(source, factory);
            var failed = await importer.DbActualizationAsync();
            Assert.NotNull(failed.Error);
            Assert.Equal(new PrintImportResult(500, 500, 0, failed.Error), failed);
            await using (var db = factory.CreateDbContext())
                Assert.Equal(500, await db.EventsSuccessfulPrinting.CountAsync(e => e.SourceKey == source.SourceKey));
            source.FailOnCall = null;
            Assert.Equal(new PrintImportResult(1, 1, 0), await importer.DbActualizationAsync());
            await using var check = factory.CreateDbContext();
            Assert.Equal(501, await check.EventsSuccessfulPrinting.CountAsync(e => e.SourceKey == source.SourceKey));
        }
        finally { await Cleanup(factory, source.SourceKey); }
    }

    [Fact]
    public async Task Concurrent_import_is_serialized()
    {
        var test = Create();
        var (factory, source) = test;
        try
        {
            source.Events = [Event(source.SourceKey, 1)];
            var results = await Task.WhenAll(
                new EventLogProcessingService(source, factory).DbActualizationAsync(),
                new EventLogProcessingService(source, factory).DbActualizationAsync());
            Assert.Equal(1, results.Sum(r => r.Added));
            await using var db = factory.CreateDbContext();
            Assert.Equal(1, await db.EventsSuccessfulPrinting.CountAsync(e => e.SourceKey == source.SourceKey));
        }
        finally { await Cleanup(factory, source.SourceKey); }
    }

    [Fact]
    public async Task Journal_reset_reuses_record_id_without_losing_new_event()
    {
        var test = Create();
        var (factory, source) = test;
        try
        {
            source.Events = [Event(source.SourceKey, 10)];
            var importer = new EventLogProcessingService(source, factory);
            Assert.Equal(1, (await importer.DbActualizationAsync()).Added);
            source.Events = [Event(source.SourceKey, 1, Time.AddMinutes(1)),
                Event(source.SourceKey, 10, Time.AddMinutes(2))];
            Assert.Equal(2, (await importer.DbActualizationAsync()).Added);
            Assert.Equal(0, (await importer.DbActualizationAsync()).Added);
            await using var db = factory.CreateDbContext();
            Assert.Equal(3, await db.EventsSuccessfulPrinting.CountAsync(e => e.SourceKey == source.SourceKey));
        }
        finally { await Cleanup(factory, source.SourceKey); }
    }

    private static (IDbContextFactory<ApplicationDbContext> Factory, FakeSource Source) Create()
    {
        var connection = Environment.GetEnvironmentVariable("DIPLOMA_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set DIPLOMA_TEST_CONNECTION to an isolated, migrated SQL Server test database before running integration tests.");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        return (new Factory(options), new FakeSource("itest-" + Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task Connection_change_between_pages_does_not_split_import_across_databases()
    {
        var (factory, source) = Create();
        using var fixture = new StoreFixture(Environment.GetEnvironmentVariable("DIPLOMA_TEST_CONNECTION"));
        fixture.MakeAdmin();
        source.Events = Enumerable.Range(1, 501).Select(i => Event(source.SourceKey, i)).ToArray();
        var importer = new EventLogProcessingService(source, new RuntimeDatabase(fixture.Store));
        importer.AddProgressChanged += (_, _, _) => fixture.Store.SaveConnection(fixture.Sid, "Server=127.0.0.1,1;Database=unavailable;Integrated Security=true;Connect Timeout=1");
        try {
            var result = await importer.DbActualizationAsync();
            Assert.Equal(new PrintImportResult(501, 501, 0), result);
            await using var check = factory.CreateDbContext();
            Assert.Equal(501, await check.EventsSuccessfulPrinting.CountAsync(e => e.SourceKey == source.SourceKey));
            Assert.Equal(501, (await check.PrintEventImportCursors.SingleAsync(e => e.SourceKey == source.SourceKey)).RecordId);
        }
        finally { await Cleanup(factory, source.SourceKey); }
    }

    [Fact]
    public async Task Same_queue_name_on_two_sources_does_not_share_target_printer()
    {
        var (factory, first) = Create(); var (_, second) = Create();
        var queueName = "printer_" + first.SourceKey;
        first.Events = [Event(first.SourceKey, 1)];
        second.Events = [new EventPrint307("doc_" + second.SourceKey, "user_" + second.SourceKey, "pc_" + second.SourceKey, queueName, 2048, 2, "test", Time) { SourceName=second.SourceKey, LogName=Log, RecordId=1 }];
        try {
            Assert.Null((await new EventLogProcessingService(first, factory).DbActualizationAsync()).Error);
            Assert.Null((await new EventLogProcessingService(second, factory).DbActualizationAsync()).Error);
            await using var check = factory.CreateDbContext();
            var ids = await check.EventsSuccessfulPrinting.Where(e => e.SourceKey == first.SourceKey || e.SourceKey == second.SourceKey).Select(e => e.TargetPrinterId).ToListAsync();
            Assert.Equal(2, ids.Distinct().Count());
        }
        finally {
            await Cleanup(factory, second.SourceKey); await Cleanup(factory, first.SourceKey);
        }
    }

    private static EventPrint307 Event(string source, long id, DateTime? time = null) =>
        new("doc_" + source, "user_" + source, "pc_" + source,
            "printer_" + source, 2048, 2, "test", time ?? Time)
        { SourceName = source, LogName = Log, RecordId = id };

    private static async Task Cleanup(IDbContextFactory<ApplicationDbContext> factory, string source)
    {
        await using var db = factory.CreateDbContext();
        await db.EventsSuccessfulPrinting.Where(e => e.SourceKey == source).ExecuteDeleteAsync();
        await db.PrintEventImportCursors.Where(e => e.SourceKey == source).ExecuteDeleteAsync();
        await db.TargetPrinters.Where(e => e.Name == "printer_" + source).ExecuteDeleteAsync();
        await db.SentPrintingFiles.Where(e => e.Name.name == "doc_" + source).ExecuteDeleteAsync();
        await db.DocumentNames.Where(e => e.name == "doc_" + source).ExecuteDeleteAsync();
        await db.Senders.Where(e => e.Name == "user_" + source).ExecuteDeleteAsync();
        await db.SenderDevices.Where(e => e.Name == "pc_" + source).ExecuteDeleteAsync();
    }

    private sealed class Factory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class FakeSource(string sourceKey) : IPrintEventSource
    {
        private int calls;
        public string SourceKey => sourceKey;
        public string LogName => Log;
        public IReadOnlyList<EventPrint307> Events { get; set; } = [];
        public int? FailOnCall { get; set; }

        public Task<PrintEventBatch> ReadBatchAsync(long? afterRecordId, DateTime? anchorTime, int limit)
        {
            if (Interlocked.Increment(ref calls) == FailOnCall)
                throw new InvalidOperationException("Test source failure");
            var events = Events;
            var lost = afterRecordId.HasValue && !events.Any(e => e.RecordId == afterRecordId && e.DateTime == anchorTime);
            var page = events.Where(e => lost || !afterRecordId.HasValue || e.RecordId > afterRecordId)
                .OrderBy(e => e.RecordId).Take(limit).ToArray();
            return Task.FromResult(new PrintEventBatch(page, lost));
        }
    }
}
