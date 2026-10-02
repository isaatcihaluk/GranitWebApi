using GranitWebApi.Models;
using GranitWebApi.Models.BakimOnarim;
using GranitWebApi.Models.IK.Izin;
using GranitWebApi.Models.Proforma;
using GranitWebApi.Models.Promanage;
using GranitWebApi.Models.Sabitler.Makine;
using GranitWebApi.Models.Sabitler.Users;
using GranitWebApi.Models.Sales;
using GranitWebApi.Models.Sales.Definitions;
using GranitWebApi.Models.Trendyol;
using GranitWebApi.Models.Ui;
using GranitWebApi.Models.XML;
using GranitWebApi.Workflow.Models;
using Microsoft.EntityFrameworkCore;

namespace GranitWebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProcessType>().ToTable("ProcessType");
            modelBuilder.Entity<ProcessRequest>().ToTable("ProcessRequest");
            modelBuilder.Entity<WorkflowDefinition>().ToTable("WorkflowDefinition");
            modelBuilder.Entity<WorkflowStep>().ToTable("WorkflowStep");
            modelBuilder.Entity<ProcessApproval>().ToTable("ProcessApproval");
            modelBuilder.Entity<WorkflowHistory>().ToTable("WorkflowHistory");

            // TECHNICIAN LOGS

            modelBuilder.Entity<TechnicianModel>()
                .Property(x => x.Duration)
                .HasConversion(
                    v => v.HasValue
                        ? (long?)v.Value.TotalSeconds
                        : null,
                    v => v.HasValue
                        ? TimeSpan.FromSeconds(v.Value)
                        : null
                );

            modelBuilder.Entity<TechnicianModel>()
                .Property(x => x.ReactionTime)
                .HasConversion(
                    v => v.HasValue
                        ? (long?)v.Value.TotalSeconds
                        : null,
                    v => v.HasValue
                        ? TimeSpan.FromSeconds(v.Value)
                        : null
                );

            // Sale Definition
            modelBuilder.Entity<ProductType>().HasNoKey();
            modelBuilder.Entity<CKProductDetailMain>().HasNoKey();
            modelBuilder.Entity<CKProductDetailColor>().HasNoKey();
            modelBuilder.Entity<SalesCustomer>()
                .HasNoKey()
                .ToView("granitVW_SALES_CUSTOMERS");

            // Sales Order Package

            modelBuilder.Entity<SalesOrderPackage>()
                .HasOne(x => x.SalesOrder)
                .WithMany(x => x.Packages)
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrderPackageLine>()
                .HasOne(x => x.SalesOrderPackage)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.SalesOrderPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrderPackageLine>()
                .HasOne(x => x.SalesOrderLine)
                .WithMany(x => x.PackageLines)
                .HasForeignKey(x => x.SalesOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);


            // Aynı siparişte aynı PackageNumber tekrar edemez
            modelBuilder.Entity<SalesOrderPackage>()
                .HasIndex(x => new
                {
                    x.SalesOrderId,
                    x.PackageNumber
                })
                .IsUnique();


            // Aynı paket içerisinde aynı sipariş kalemi tekrar edemez
            modelBuilder.Entity<SalesOrderPackageLine>()
                .HasIndex(x => new
                {
                    x.SalesOrderPackageId,
                    x.SalesOrderLineId
                })
                .IsUnique();


            // Decimal alan
            modelBuilder.Entity<SalesOrderPackageLine>().Property(x => x.Quantity).HasPrecision(18, 3);
            modelBuilder.Entity<PaketRenk>().HasKey(x => x.No);

            // PROFORMA
            modelBuilder.Entity<SalesProforma>().HasMany(x => x.Lines).WithOne(x => x.Proforma)
                .HasForeignKey(x => x.ProformaId).OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<TechnicianModel> TechnicianLogs { get; set; }
        public DbSet<TechnicianMaintenanceIssueType> TechnicianMaintenanceIssueTypes { get; set; }
        public DbSet<TechnicianMaintenanceActionType> TechnicianMaintenanceActionTypes { get; set; }
        public DbSet<TechnicianMaintenanceIssue> TechnicianMaintenanceIssues { get; set; }
        public DbSet<TechnicianMaintenanceAction> TechnicianMaintenanceActions { get; set; }
        public DbSet<TechnicianMaintenanceStatusHistory> TechnicianMaintenanceStatusHistory { get; set; }
        public DbSet<TechnicianMaintenancePart> TechnicianMaintenanceParts { get; set; }
        public DbSet<Makine> Makine { get; set; }
        public DbSet<MakineBilesen> MakineBilesenler { get; set; }
        public DbSet<MakineBilesenHaritasi> MakineBilesenHaritasi { get; set; }
        public DbSet<TechnicianLogTechnicians> TechnicianLogTechnicians { get; set; }

        //trendyol
        public DbSet<TyOrderHeader> TyOrderHeaders { get; set; }
        public DbSet<TyOrderLine> TyOrderLines { get; set; }
        public DbSet<TyOrderAddress> TyOrderAddresses { get; set; }
        public DbSet<TyPackageHistory> TyPackageHistories { get; set; }
        public DbSet<TyOrderRaw> TyOrderRaws { get; set; }
        public DbSet<TyIntegrationLog> TyIntegrationLogs { get; set; }

        // Workflow
        public DbSet<ProcessType> ProcessTypes { get; set; }
        public DbSet<ProcessRequest> ProcessRequest { get; set; }
        public DbSet<ProcessApproval> ProcessApproval { get; set; }
        public DbSet<WorkflowDefinition> WorkflowDefinition { get; set; }
        public DbSet<WorkflowStep> WorkflowStep { get; set; }
        public DbSet<WorkflowResolver> WorkflowResolver { get; set; }
        public DbSet<WorkflowHistory> WorkflowHistory { get; set; }
        // Izın
        public DbSet<IzinTurleri> IzinTurleri { get; set; }
        public DbSet<IzinTalepleri> IzinTalepleri { get; set; }
        public DbSet<IzinYillikBakiyePersonel> IzinYillikBakiyePersonel { get; set; }
        public DbSet<EmailQueue> EmailQueue { get; set; }
        // Upload
        public DbSet<PromanageOee> PromanageOee { get; set; }
        public DbSet<PromanageGunlukUretim> PromanageGunlukUretim { get; set; }
        public DbSet<PromanageOee> DowntimeSummaryResult { get; set; }
        public DbSet<PromanageGunlukUretim> DowntimeMachineResult { get; set; }
        public DbSet<PromanageGunlukUretim> DowntimeTrendResult { get; set; }
        public DbSet<PromanageGunlukUretim> DowntimeReasonResult { get; set; }
        public DbSet<PromanageStops> PromanageStops { get; set; }
        public DbSet<PromanageScrap> PromanageScraps { get; set; }

        // UI && USER
        public DbSet<UiPage> UiPages => Set<UiPage>();
        public DbSet<UiPagePermission> UiPagePermissions { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // Sales Order
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderLine> SalesOrderLines { get; set; }
        public DbSet<SalesOrderLineCKConfiguration> SalesOrderLineCKConfigurations { get; set; }
        public DbSet<SalesOrderImageType> SalesOrderImageTypes { get; set; }
        public DbSet<SalesOrderLineImage> SalesOrderLineImages { get; set; }
        public DbSet<SalesOrderLineTechnicalItem> SalesOrderLineTechnicalItems { get; set; }
        public DbSet<SalesOrderAssemblyCodeRequest> SalesOrderAssemblyCodeRequests { get; set; }
        public DbSet<SalesOrderPackage> SalesOrderPackages { get; set; }
        public DbSet<SalesOrderPackageLine> SalesOrderPackageLines { get; set; }
        public DbSet<SalesOrderLineUMConfiguration> SalesOrderLineUMConfigurations { get; set; }

        // Sales Definition
        public DbSet<ProductGroup> ProductGroups { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<CKProductDetailMain> CKProductDetailMains { get; set; }
        public DbSet<CKProductDetailColor> CKProductDetailColors { get; set; }
        public DbSet<PlasticColor> PlasticColors { get; set; }
        public DbSet<ProductBox> ProductBoxes { get; set; }
        public DbSet<SalesCustomer> SalesCustomers { get; set; }
        public DbSet<PaketRenk> PaketRenkler { get; set; } = null!;
        public DbSet<ProductShrink> ProductShrinks { get; set; }
        public DbSet<UmUrunDetayAna> UmUrunDetayAna { get; set; }

        // PROFORMA
        public DbSet<SalesProforma> SalesProformas { get; set; }
        public DbSet<SalesProformaLine> SalesProformaLines { get; set; }
        public DbSet<SalesProformaPayment> SalesProformaPayments { get; set; }
    }
}
