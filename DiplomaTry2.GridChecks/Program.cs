using System.Security.Claims;
using System.IO;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using DiplomaTry2.GridChecks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDevExpressBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, CheckAuthentication>();
var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../DiplomaTry2/wwwroot")))
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<TestApp>().AddInteractiveServerRenderMode();
app.Run();

sealed class CheckAuthentication : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(
        new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "grid-check-user")], "GridCheck"))));
}
