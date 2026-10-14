using System.Security.Claims;
using DiplomaTry2.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Services;

public sealed class WindowsRoleClaimsTransformation(ServiceStore store)
    : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var account = FirstRunAdminSetup.WindowsAccount(principal);
        if (account is null) return Task.FromResult(principal);
        var role = store.Register(account.Value.Sid, account.Value.Name);
        var transformed = principal.Clone();
        foreach (var old in transformed.Identities.Where(i => i.AuthenticationType == "DiplomaTry2Roles").ToArray())
            foreach (var claim in old.Claims.ToArray()) old.RemoveClaim(claim);
        var identity = new ClaimsIdentity("DiplomaTry2Roles");
        identity.AddClaim(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, "DiplomaTry2"));
        transformed.AddIdentity(identity);
        return Task.FromResult(transformed);
    }
}

