using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DiplomaTry2.Data;

public sealed class ServiceAccount
{
    public int Id { get; set; }
    public string Sid { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "User";
}

public sealed class ServiceSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class ServiceRole
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class ServicePrintServer
{
    public int Id { get; set; }
    public string Host { get; set; } = "";
    public string HostKey { get; set; } = "";
}

public sealed class ServiceDatabaseConnection
{
    public int Id { get; set; }
    public string ConnectionKey { get; set; } = "";
    public string ProtectedConnection { get; set; } = "";
}

// The local service database has its own model and migrations, independent of SQL Server.
public sealed class ServiceDbContext(DbContextOptions<ServiceDbContext> options) : DbContext(options)
{
    public DbSet<ServiceAccount> Accounts => Set<ServiceAccount>();
    public DbSet<ServiceSetting> Settings => Set<ServiceSetting>();
    public DbSet<ServiceRole> Roles => Set<ServiceRole>();
    public DbSet<ServicePrintServer> PrintServers => Set<ServicePrintServer>();
    public DbSet<ServiceDatabaseConnection> DatabaseConnections => Set<ServiceDatabaseConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceDatabaseConnection>(entity => {
            entity.ToTable("DatabaseConnections");
            entity.Property(c => c.ConnectionKey).HasMaxLength(64);
            entity.HasIndex(c => c.ConnectionKey).IsUnique();
        });
        modelBuilder.Entity<ServiceAccount>(entity => {
            entity.ToTable("WindowsAccounts");
            entity.Property(a => a.Sid).HasMaxLength(184);
            entity.Property(a => a.Name).HasMaxLength(256);
            entity.Property(a => a.Role).HasMaxLength(16);
            entity.HasIndex(a => a.Sid).IsUnique();
        });
        modelBuilder.Entity<ServiceSetting>(entity => {
            entity.ToTable("GlobalSettings");
            entity.Property(s => s.Key).HasMaxLength(100);
            entity.HasIndex(s => s.Key).IsUnique();
        });
        modelBuilder.Entity<ServiceRole>(entity => {
            entity.ToTable("ApplicationRoles");
            entity.Property(r => r.Name).HasMaxLength(16);
            entity.HasIndex(r => r.Name).IsUnique();
        });
        modelBuilder.Entity<ServicePrintServer>(entity => {
            entity.ToTable("PrintServers");
            entity.Property(s => s.Host).HasMaxLength(190);
            entity.Property(s => s.HostKey).HasMaxLength(190);
            entity.HasIndex(s => s.HostKey).IsUnique();
        });
    }
}

// EF tooling must not start the web host, initialize ServiceStore, or contact SQL Server.
public sealed class ServiceDbContextDesignFactory : IDesignTimeDbContextFactory<ServiceDbContext>
{
    public ServiceDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<ServiceDbContext>().UseSqlite("Data Source=:memory:").Options);
}
