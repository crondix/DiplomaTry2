using System.Data;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DiplomaTry2.Data;

public sealed class FirstRunAdminSetup(ServiceStore store, IConfiguration configuration)
{
    public static (string Sid, string Name)? WindowsAccount(ClaimsPrincipal principal)
    {
        var identity = principal.Identities.OfType<WindowsIdentity>()
            .FirstOrDefault(i => i.IsAuthenticated && i.User is not null);
        return identity?.User?.Value is { } sid && !string.IsNullOrWhiteSpace(identity.Name)
            ? (sid, identity.Name)
            : null;
    }

    public static bool IsLocalRequest(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        return address is not null && IPAddress.IsLoopback(
            address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);
    }

    public Task<bool> HasWindowsAdminAsync() => Task.FromResult(store.HasAdmin());

    public Task<bool> ClaimAsync(string sid, string name)
    {
        _ = new SecurityIdentifier(sid);
        return Task.FromResult(store.Claim(sid, name, configuration["ServiceDatabase:BootstrapAdminSid"]));
    }
}
