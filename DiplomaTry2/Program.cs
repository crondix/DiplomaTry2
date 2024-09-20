using DiplomaTry2.InterFaces;
using DiplomaTry2.Services;
using DiplomaTry2.Client.Pages;
using DiplomaTry2.Components;
using DiplomaTry2.Components.Account;
using DiplomaTry2.Data;

using DevExpress.Blazor;


using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DiplomaTry2.Components.Pages;
using System.Net.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Components;
namespace DiplomaTry2
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
           
            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
     
            builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
           
            builder.Services.AddDevExpressBlazor(configure => configure.BootstrapVersion = BootstrapVersion.v5);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContextFactory<ApplicationDbContext>(options => {
                options.EnableSensitiveDataLogging();
                options.UseSqlServer(
                    connectionString, sqlServerOptions =>
                    sqlServerOptions.EnableRetryOnFailure()
                );}
            );

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


            
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress =  new Uri("https://localhost:7197/") });
            builder.Services.AddDevExpressBlazor(configure => configure.BootstrapVersion = BootstrapVersion.v5);
            builder.Services.AddScoped<PrintServerService>();
            builder.Services.AddScoped<EventLogService>();
            builder.Services.AddScoped<EventLogProcessingService>();
            builder.Services.AddScoped<NetPrintersService>();
            builder.Services.AddScoped<ComparisonService>();
           
           

            


            var app = builder.Build();
           
            // Apply migrations at startup
            //using (var scope = app.Services.CreateScope())
            //{
            //    var services = scope.ServiceProvider;
            //    var dbContext = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();
            //    dbContext.Database.Migrate();

            //    //var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            //    //await SeedData.Initialize(services, userManager);
            //}
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var dbContext = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();

                // ѕроверка, существует ли база данных
                if (dbContext.Database.CanConnect())
                {
                    var migrator = dbContext.GetService<IMigrator>();
                    var pendingMigrations = dbContext.Database.GetPendingMigrations();

                    // ѕрименение миграций только если есть неприеменные миграции
                    if (pendingMigrations.Any())
                    {
                        dbContext.Database.Migrate();
                    }
                }

                //var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                //await SeedData.Initialize(services, userManager);
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
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();



            //app.MapGet("/printerList", () =>
            //{
            //    PrintServerService printServerService = new PrintServerService();
            //    return Results.Json(printServerService.GetListNetPrintersInfoFromPrintServer(@$"{app.Configuration["printserver:name"].ToString()}"));
            //});

            //app.MapGet("/printerModelsList", () =>
            //{
            //    PrintServerService printServerService = new PrintServerService();
            //    return Results.Json(printServerService.GetPrintersModelsList(@$"{app.Configuration["printserver:name"].ToString()}"));
            //});




            app.Run();
        }
    }
}
