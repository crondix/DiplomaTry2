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
        public DbSet<NetworkPrinter> NetworkPrinters { get; set; }
        public DbSet<PrinterModel> PrinterModels { get; set; }
        public DbSet<PaperSize> PaperSizes { get; set; }
        public DbSet<PrintEvent> PrintEvents { get; set; }
        public DbSet<PrintserverEvent> PrintserverEvents { get; set; }
        public DbSet<SenderDevice> SenderDevice { get; set; }
        public DbSet<SentPrintingFile> SentPrintingFile { get; set; }
        public DbSet<Sender> Sender { get; set; }
       

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
