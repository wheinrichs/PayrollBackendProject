using Microsoft.EntityFrameworkCore;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Infrastructure.Data
{
    public class ClinicianDbContext: DbContext
    {
        public DbSet<Clinician> Clinicians { get; set; }
        public DbSet<UserAccount> Users { get; set; }
        public DbSet<ImportBatch> ImportBatches {  get; set; }
        public DbSet<PaymentLineItem> PaymentLineItems { get; set; }
        public DbSet<EHRUser> EHRUsers { get; set; }
        public DbSet<PayRun> PayRuns { get; set; }
        public DbSet<PayStatement> PayStatements { get; set; }
        public DbSet<PaymentSnapshot> PaymentSnapshots { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public ClinicianDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Clinician>().HasIndex(c => c.Email).IsUnique();

            modelBuilder.Entity<ImportBatch>().HasIndex(batch => batch.Fingerprint).IsUnique();

            modelBuilder.Entity<PaymentLineItem>().HasIndex(line => line.Fingerprint).IsUnique();
        }
    }
}
