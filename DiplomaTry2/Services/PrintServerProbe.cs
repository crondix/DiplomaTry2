using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DiplomaTry2.Services;

public sealed record ProbeStep(bool Success, string Detail);
public sealed record PrintServerProbeResult(string Host, string? Address, ProbeStep Dns, ProbeStep Ping, ProbeStep Rpc, DateTimeOffset CheckedAt);

public interface IPrintServerNetworkProbe
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken token);
    Task<ProbeStep> PingAsync(IPAddress address, CancellationToken token);
    Task<ProbeStep> RpcAsync(IPAddress address, CancellationToken token);
}

// No printer enumeration, event log access, credentials or application data.
public sealed class PrintServerNetworkProbe : IPrintServerNetworkProbe
{
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken token) =>
        IPAddress.TryParse(host, out var address) ? Task.FromResult(new[] { address }) : Dns.GetHostAddressesAsync(host, token);
    public async Task<ProbeStep> PingAsync(IPAddress address, CancellationToken token)
    {
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(address, TimeSpan.FromSeconds(2), new byte[32], cancellationToken: token);
        return new(reply.Status == IPStatus.Success, reply.Status == IPStatus.Success ? $"Ответ за {reply.RoundtripTime} мс" : "Ответ не получен (возможно, ICMP заблокирован)");
    }
    public async Task<ProbeStep> RpcAsync(IPAddress address, CancellationToken token)
    {
        using var client = new TcpClient(address.AddressFamily);
        await client.ConnectAsync(address, 135, token);
        return new(true, "TCP-порт 135 доступен");
    }
}

// One bounded DNS request, one 32-byte echo and one TCP connect. Ping and TCP run
// together. A global gate limits concurrent checks across browser circuits.
public sealed class PrintServerProbe(IPrintServerNetworkProbe network, TimeSpan? stepTimeout = null) : IDisposable
{
    private readonly SemaphoreSlim gate = new(4);
    private readonly TimeSpan timeout = stepTimeout ?? TimeSpan.FromSeconds(2);
    public async Task<PrintServerProbeResult> CheckAsync(string host, CancellationToken token = default)
    {
        host = PrintSourceSelection.Validate("Windows", host, "Default").ServerName;
        if (!await gate.WaitAsync(TimeSpan.FromSeconds(1), token))
            throw new InvalidOperationException("Уже выполняется несколько проверок. Повторите через несколько секунд.");
        try {
            IPAddress[] addresses;
            try { addresses = await Bounded(ct => network.ResolveAsync(host, ct), token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or TimeoutException) {
                return new(host, null, new(false, "Адрес не найден или DNS не ответил за 2 секунды"), new(false, "Не выполнялся"), new(false, "Не выполнялась"), DateTimeOffset.UtcNow);
            }
            var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses.FirstOrDefault();
            if (address is null) return new(host, null, new(false, "IP-адрес не найден"), new(false, "Не выполнялся"), new(false, "Не выполнялась"), DateTimeOffset.UtcNow);
            var ping = Step(ct => network.PingAsync(address, ct), "Нет ответа (возможно, ICMP заблокирован)", token);
            var rpc = Step(ct => network.RpcAsync(address, ct), "TCP-порт 135 не ответил или недоступен", token);
            await Task.WhenAll(ping, rpc);
            return new(host, address.ToString(), new(true, "IP-адрес определён"), await ping, await rpc, DateTimeOffset.UtcNow);
        }
        finally { gate.Release(); }
    }
    private async Task<T> Bounded<T>(Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(timeout);
        try { return await action(deadline.Token).WaitAsync(timeout, token); }
        finally { await deadline.CancelAsync(); }
    }
    private async Task<ProbeStep> Step(Func<CancellationToken, Task<ProbeStep>> action, string failure, CancellationToken token)
    {
        try { return await Bounded(action, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is PingException or SocketException or OperationCanceledException or TimeoutException or PlatformNotSupportedException)
        { return new(false, failure); }
    }
    public void Dispose() => gate.Dispose();
}
