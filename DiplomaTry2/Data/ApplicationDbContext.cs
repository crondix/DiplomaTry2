using DiplomaTry2.Client.Pages;
using Humanizer.Localisation;
using System.Security.Policy;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using DiplomaModels;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;

namespace DiplomaTry2.Data
{
    //public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    //{
    //    public DbSet<NetworkPrinter> Baskets { get; set; }
    //    public DbSet<PrinterModel> Books { get; set; }
    //    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    //  : base(options)
    //    {
    //    }

    //}
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<NetworkPrinter> NetworkPrinters { get; set; } = null!;
        public DbSet<PrinterModel> PrinterModels { get; set; } = null!;
        public DbSet<PaperSize> PaperSizes { get; set; } = null!;
        public DbSet<EventSuccessfulPrinting> EventsSuccessfulPrinting { get; set; } = null!;
        public DbSet<PrintserverEvent> PrintserverEvents { get; set; } = null!;
        public DbSet<SenderDevice> SenderDevices { get; set; } = null!;
        public DbSet<SentPrintingFile> SentPrintingFiles { get; set; } = null!;
        public DbSet<Sender> Senders { get; set; } = null!;
        public DbSet<TargetPrinter> TargetPrinters { get; set; } = null!;
        public DbSet<DocumentName> DocumentNames { get; set; } = null!;
        public DbSet<ChangeLog> ChangeLogs { get; set; } = null!;


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          

            base.OnModelCreating(modelBuilder);

          
        }

        
    }
}
