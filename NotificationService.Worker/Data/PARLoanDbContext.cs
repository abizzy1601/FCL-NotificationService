using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Worker.Data.Entities;
using NotificationService.Worker.DTOs;
using NotificationService.Worker.Settings;

namespace NotificationService.Worker.Data
{
    public class PARLoanDbContext : DbContext
    {
        private readonly string _tableName;

        public PARLoanDbContext(DbContextOptions<PARLoanDbContext> options, IOptions<LoanReminderSettings> settings)
            : base(options)
        {
            _tableName = settings.Value.SourceTableName;
        }

        public DbSet<LoanAccount> LoanAccounts { get; set; }

        public DbSet<LoanReminderRecord> LoanReminderRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configured inline (not via IEntityTypeConfiguration) to prevent ApplicationDbContext's
            // ApplyConfigurationsFromAssembly scan from picking these up and registering them there.

            modelBuilder.Entity<LoanAccount>(entity =>
            {
                entity.ToTable(_tableName);
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CustomerName).HasColumnName("Customer_Name");
                entity.Property(x => x.PhoneNo).HasColumnName("Phone_No");
                entity.Property(x => x.DisbursementDate).HasColumnName("Disbursement_Date");
                entity.Property(x => x.MaturityDate).HasColumnName("Maturity_Date");
                entity.Property(x => x.MonthlyInstalment).HasColumnName("Monthly_Instalment");
                entity.Property(x => x.PrincipalBalance).HasColumnName("Principal_Balance");
                entity.Property(x => x.LoanAmount).HasColumnName("Loan_Amount");
                entity.Property(x => x.OdStatus).HasColumnName("OD_Status");
                entity.Property(x => x.ReportDate).HasColumnName("Report_Date");
                entity.Property(x => x.Product).HasColumnName("Product");
            });

            modelBuilder.Entity<LoanReminderRecord>(entity =>
            {
                entity.ToTable("LoanReminderRecords");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Arrangement).IsRequired().HasMaxLength(50);
                entity.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(x => x.NotificationType)
                      .HasConversion<string>()
                      .IsRequired()
                      .HasMaxLength(20);
                entity.Property(x => x.ErrorMessage).HasMaxLength(500);
                entity.HasIndex(x => new { x.LoanId, x.CycleYear, x.CycleMonth, x.NotificationType })
                      .HasDatabaseName("IX_LoanReminderRecords_Loan_Cycle_Type");
            });
        }
    }
}
