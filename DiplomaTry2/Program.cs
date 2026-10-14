using DiplomaTry2.InterFaces;
using DiplomaTry2.Services;
using DiplomaTry2.Client.Pages;
using DiplomaTry2.Components;
using DiplomaTry2.Components.Account;
using DiplomaTry2.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.IO;
using DevExpress.Blazor;
using Microsoft.AspNetCore.DataProtection;
namespace DiplomaTry2
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                await RunAsync(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Не удалось запустить DiplomaTry2: {ex.GetBaseException().Message}");
                Environment.ExitCode = 1;
            }
        }

        private static async Task RunAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseStaticWebAssets();
            // A restricted interactive account cannot write to the Windows Event Log.
            // Keep startup failures visible in the console instead of masking them with a logging exception.
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            var logPath = builder.Configuration["ApplicationLogs:Directory"] ?? "%ProgramData%/DiplomaTry2/logs";
            builder.Services.AddSingleton(_ => new ApplicationLogStore(logPath));
            builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<ApplicationLogStore>());
            builder.Services.AddScoped<ApplicationLogService>();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();
            builder.Services.AddDevExpressBlazor();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
            builder.Services.AddScoped<IClaimsTransformation, WindowsRoleClaimsTransformation>();
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            });

            var servicePath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(builder.Configuration["ServiceDatabase:Path"] ?? "%ProgramData%/DiplomaTry2/service.sqlite"));
            builder.Services.AddDataProtection().SetApplicationName("DiplomaTry2")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetDirectoryName(servicePath)!, "keys")))
                .ProtectKeysWithDpapi();
            builder.Services.AddSingleton<ServiceStore>();
            builder.Services.AddSingleton<RuntimeDatabase>();
            builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp => sp.GetRequiredService<RuntimeDatabase>());
            builder.Services.AddScoped(sp => sp.GetRequiredService<RuntimeDatabase>().CreateDbContext());
            builder.Services.AddSingleton<LegacyRoleMigration>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<LegacyRoleMigration>());
            builder.Services.AddScoped<ApplicationSettingsService>();
            builder.Services.AddSingleton<IMainDatabaseValidator, MainDatabaseValidator>();
            builder.Services.AddSingleton<IPrintServerNetworkProbe, PrintServerNetworkProbe>();
            builder.Services.AddSingleton(sp => new PrintServerProbe(sp.GetRequiredService<IPrintServerNetworkProbe>()));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddScoped<FirstRunAdminSetup>();



            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7197/") });

            var printSourceSettings = PrintSourceSettings.FromConfiguration(builder.Configuration);
            builder.Services.AddSingleton(printSourceSettings);
            builder.Services.AddScoped<PrintSourceSelection>();
            builder.Services.AddScoped<PrintSourcePreferences>();
            builder.Services.AddScoped<FixturePrintSource>();
            builder.Services.AddScoped<WindowsPrinterSource>();
            builder.Services.AddScoped<WindowsPrintEventSource>();
            builder.Services.AddScoped<IPrinterSource, SelectedPrinterSource>();
            builder.Services.AddScoped<IPrintEventSource, SelectedPrintEventSource>();
            builder.Services.AddScoped<EventLogProcessingService>();
            builder.Services.AddScoped<NetPrintersService>();
            builder.Services.AddScoped<PrinterModelService>();






            await using var app = builder.Build();
            try
            {
                app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Приложение запущено. Среда: {Environment}.", app.Environment.EnvironmentName));
                app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Приложение останавливается."));

                // Main database availability must never prevent Windows authentication or settings.
                app.Services.GetRequiredService<ServiceStore>();
                if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
                {
                    await DatabaseStartup.CheckOrMigrateAsync(app.Services, builder.Configuration, args);
                    return;
                }

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseWebAssemblyDebugging();
                    app.UseMigrationsEndPoint();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseAntiforgery();

                // Legacy password and cookie pages are retained as source but are not routes in Windows mode.
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    await next();
                });

                app.MapStaticAssets();

                app.MapRazorComponents<App>()
                    .AddInteractiveServerRenderMode()
                    .AddInteractiveWebAssemblyRenderMode()
                    .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

                app.MapPost("/Setup/claim", async (HttpContext context, FirstRunAdminSetup setup, IAntiforgery antiforgery) =>
                {
                    if (!FirstRunAdminSetup.IsLocalRequest(context)) return (IResult)Results.Forbid();
                    var account = FirstRunAdminSetup.WindowsAccount(context.User);
                    if (account is null) return Results.Forbid();
                    try { await antiforgery.ValidateRequestAsync(context); }
                    catch (AntiforgeryValidationException) { return Results.BadRequest("Invalid setup form token."); }
                    try {
                        return await setup.ClaimAsync(account.Value.Sid, account.Value.Name)
                            ? Results.Redirect("/settings") : Results.Conflict("Администратор уже назначен.");
                    }
                    catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
                }).RequireAuthorization();



                app.Run();
            }
            catch (Exception ex)
            {
                app.Logger.LogCritical(ex, "Приложение завершилось с ошибкой.");
                throw;
            }
        }
    }
}
