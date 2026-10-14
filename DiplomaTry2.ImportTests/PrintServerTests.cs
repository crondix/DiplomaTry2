using System.Net;
using System.Net.Sockets;
using DiplomaTry2.Services;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class PrintServerTests
{
    [Fact] public async Task Servers_are_saved_without_duplicates_and_switch_is_global_with_frozen_operations()
    {
        using var f = new StoreFixture(); f.MakeAdmin();
        Assert.Equal("initial-print", Assert.Single(f.Store.PrintServers()).Host);
        var service = f.Settings();
        var first = new PrintSourceSelection(new(), f.Store); var second = new PrintSourceSelection(new(), f.Store);
        var snapshot = first.Capture();
        await service.AddPrintServerAsync(@" \\PRINT-02 ");
        await service.AddPrintServerAsync("print-02");
        Assert.Equal(2, service.PrintServers().Count);
        Assert.Equal("initial-print", first.ServerName);
        await service.ActivatePrintServerAsync("print-02");
        Assert.Equal("PRINT-02", first.ServerName); Assert.Equal(first.ServerName, second.ServerName);
        Assert.Equal("initial-print", snapshot.ServerName);
        using var reopened = f.Open();
        Assert.Equal(2, reopened.PrintServers().Count);
        Assert.Equal("PRINT-02", Assert.Single(reopened.PrintServers().Where(s => s.IsActive)).Host);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemovePrintServerAsync("print-02"));
        await service.RemovePrintServerAsync("initial-print");
        using var again = f.Open(); Assert.Single(again.PrintServers());
        await service.AddPrintServerAsync("initial-print");
        Assert.Equal(2, service.PrintServers().Count);
        first.Dispose(); second.Dispose();
    }

    [Fact] public async Task Unknown_or_invalid_server_does_not_replace_active_source()
    {
        using var f = new StoreFixture(); f.MakeAdmin(); var service = f.Settings();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivatePrintServerAsync("unsaved-print"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddPrintServerAsync("server/queue"));
        Assert.Equal("initial-print", f.Store.Snapshot().PrintServer);
        Assert.Single(f.Store.PrintServers());
    }

    [Fact] public async Task User_cannot_manage_servers_or_initiate_network_checks()
    {
        using var f = new StoreFixture(); f.Store.Register(f.Sid, f.Name);
        var network = new FakeNetwork(); using var probe = new PrintServerProbe(network); var service = f.Settings(probe: probe);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AddPrintServerAsync("another"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ActivatePrintServerAsync("initial-print"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RemovePrintServerAsync("initial-print"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CheckPrintServerAsync("initial-print"));
        Assert.Equal(0, network.Calls);
        Assert.Single(service.PrintServers());
    }

    [Fact] public async Task Check_reports_ping_and_rpc_independently_and_does_not_change_settings()
    {
        using var f = new StoreFixture(); f.MakeAdmin();
        var network = new FakeNetwork { PingResult = new(false, "ICMP заблокирован"), RpcResult = new(true, "Порт доступен") };
        using var probe = new PrintServerProbe(network);
        var before = f.Store.Snapshot(); var result = await f.Settings(probe: probe).CheckPrintServerAsync("probe-host");
        Assert.True(result.Dns.Success); Assert.False(result.Ping.Success); Assert.True(result.Rpc.Success);
        Assert.Equal("192.0.2.1", result.Address); Assert.Equal(3, network.Calls);
        Assert.Equal(before, f.Store.Snapshot()); Assert.Single(f.Store.PrintServers());
    }

    [Fact] public async Task Dns_failure_skips_ping_and_rpc()
    {
        var network = new FakeNetwork { DnsFailure = true }; using var probe = new PrintServerProbe(network);
        var result = await probe.CheckAsync("unavailable");
        Assert.False(result.Dns.Success); Assert.Null(result.Address); Assert.Equal(1, network.Calls);
    }

    [Fact] public async Task Check_has_bounded_wait_and_honors_cancellation()
    {
        var network = new FakeNetwork { WaitForever = true }; using var probe = new PrintServerProbe(network, TimeSpan.FromMilliseconds(50));
        var result = await probe.CheckAsync("unavailable"); Assert.False(result.Dns.Success);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => probe.CheckAsync("unavailable", cancellation.Token));
    }

    private sealed class FakeNetwork : IPrintServerNetworkProbe
    {
        public int Calls;
        public bool DnsFailure, WaitForever;
        public ProbeStep PingResult = new(true, "Ответ"), RpcResult = new(true, "Порт доступен");
        public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken token) {
            Interlocked.Increment(ref Calls);
            if (WaitForever) await Task.Delay(Timeout.Infinite, token);
            if (DnsFailure) throw new SocketException();
            return [IPAddress.Parse("192.0.2.1")];
        }
        public Task<ProbeStep> PingAsync(IPAddress address, CancellationToken token) { Interlocked.Increment(ref Calls); return Task.FromResult(PingResult); }
        public Task<ProbeStep> RpcAsync(IPAddress address, CancellationToken token) { Interlocked.Increment(ref Calls); return Task.FromResult(RpcResult); }
    }
}
