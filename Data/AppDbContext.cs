using GranitWebApi.Models;
using GranitWebApi.Models.BakimOnarim;
using GranitWebApi.Models.IK.Izin;
using GranitWebApi.Models.Promanage;
using GranitWebApi.Models.Sabitler.Makine;
using GranitWebApi.Models.Sabitler.Users;
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
    }
}
