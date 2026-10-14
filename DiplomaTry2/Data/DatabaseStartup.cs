using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Data;

internal static class DatabaseStartup
{
    internal static string GetConnectionString(IConfiguration configuration)
    {
        var value = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection (or ConnectionStrings__DefaultConnection) before starting the application.");

        var timeout = configuration.GetValue<int?>("Database:ConnectionTimeoutSeconds") ?? 5;
        if (timeout is < 1 or > 30)
            throw new InvalidOperationException("Database:ConnectionTimeoutSeconds must be between 1 and 30.");

        return new SqlConnectionStringBuilder(value) { ConnectTimeout = timeout }.ConnectionString;
    }

    // Returns true when the explicit migration command has completed and hosting should stop.
    internal static async Task<bool> CheckOrMigrateAsync(IServiceProvider services, IConfiguration configuration, string[] args)
    {
        var connection = new SqlConnectionStringBuilder(services.GetRequiredService<ServiceStore>().Snapshot().ConnectionString);
        using var scope = services.CreateScope();
        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
            .CreateDbContextAsync();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await db.Database.OpenConnectionAsync();
            if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
            {
                var expected = db.Database.GetMigrations().ToArray();
                var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                if (!applied.SequenceEqual(expected.Take(applied.Length)))
                    throw new InvalidOperationException("Migration history does not match the current migration chain. Inspect __EFMigrationsHistory and the schema before updating this database.");

                if (applied.Length == 0)
                {
                    await using var command = db.Database.GetDbConnection().CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name IN ('AspNetUsers', 'NetworkPrinters')";
                    command.CommandTimeout = 15;
                    if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
                        throw new InvalidOperationException("Tables exist without migration history. Inspect the database before applying migrations.");
                }

                logger.LogInformation("Applying {Count} pending migrations to {Server}/{Database}.", expected.Length - applied.Length, connection.DataSource, connection.InitialCatalog);
                await db.Database.CloseConnectionAsync();
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migration complete.");
                return true;
            }

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
            if (pending.Length > 0)
                throw new InvalidOperationException($"Database has pending migrations: {string.Join(", ", pending)}. Apply them explicitly with --migrate after reviewing the database.");

            var modelCount = await db.PrinterModels.CountAsync();
            var printerCount = await db.NetworkPrinters.CountAsync();
            var eventCount = await db.EventsSuccessfulPrinting.CountAsync();
            logger.LogInformation("Database ready: {Server}/{Database}. Models: {Models}, printers: {Printers}, print events: {Events}.",
                connection.DataSource, connection.InitialCatalog, modelCount, printerCount, eventCount);
            return false;
        }
        catch (Exception ex) when (ex is SqlException || ex.InnerException is SqlException)
        {
            var sqlError = (SqlException)(ex as SqlException ?? ex.InnerException!);
            logger.LogError("SQL Server operation failed for {Server}/{Database} (SQL error {Number}).", connection.DataSource, connection.InitialCatalog, sqlError.Number);
            throw new InvalidOperationException($"Cannot use SQL Server database {connection.DataSource}/{connection.InitialCatalog}. Check server access and the connection string. SQL error {sqlError.Number}.", ex);
        }
    }
}
