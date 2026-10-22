using FullVoucherSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FullVoucherSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<BillingCertification> BillingCertifications { get; set; }
        public DbSet<ImprestHolder> Imprest_Holder { get; set; }
        public DbSet<CashRoom> CashRooms { get; set; }
        public DbSet<Advance> Advances { get; set; }
        public DbSet<FinalLedger> Final_Ledger { get; set; }
        public DbSet<Audit> Audits { get; set; }
        public DbSet<BillingQuery> BillingQueries { get; set; }
        public DbSet<QueryInformation> QueryInformations { get; set; }
        public DbSet<AuditQueryInformation> AuditQueryInformations { get; set; }
        public DbSet<IgrFundedCapitalBudget> IgrFundedCapitalBudgets { get; set; }
        public DbSet<OtherClassificationBudget> OtherClassificationBudgets { get; set; }

        public DbSet<StaffAdvanceBudget> StaffAdvanceBudgets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IgrFundedCapitalBudget>()
                .ToTable("IgrFundedCapitalBudget", "dbo")
                .HasKey(b => b.Id);

            modelBuilder.Entity<OtherClassificationBudget>()
                .ToTable("OtherClassificationBudget", "dbo")
                .HasKey(b => b.Id);

            modelBuilder.Entity<Voucher>()
                .HasIndex(v => v.VoucherNumber)
                .IsUnique();


            modelBuilder.Entity<StaffAdvanceBudget>(entity =>
            {
                entity.ToTable("StaffAdvanceBudget", "dbo");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Payee)
                    .HasMaxLength(250)
                    .IsRequired();

                entity.Property(x => x.VoteRefNo)
                    .HasColumnName("VoteRefNo")
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(x => x.BudgetAmount)
                    .HasColumnName("BudgetAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired(false);

                entity.Property(x => x.DateUpdated)
                    .IsRequired();

                //entity.HasIndex(x => x.Payee)
                //    .IsUnique();
            });

        }

        public override int SaveChanges()
        {
            UpdateDateTimeValues();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateDateTimeValues();
            return base.SaveChangesAsync(cancellationToken);
        }


        private void UpdateDateTimeValues()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue != null)
                    {
                        var dateTimeValue = (DateTime)property.CurrentValue;

                        if (dateTimeValue < new DateTime(1753, 1, 1))
                        {
                            property.CurrentValue = DateTime.Now;
                        }
                    }
                }
            }
        }
    }
}