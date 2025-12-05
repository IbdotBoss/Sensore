using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sensore.Models;

namespace Sensore.Data
{
    // Generics are: <UserType, RoleType, KeyType>
    // KeyType for default Identity is string
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<PressureFrame> PressureFrames { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ClinicianPatientMap> ClinicianPatientMaps { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Initializes Identity tables
            base.OnModelCreating(builder);

            // 1. Configure Many-to-Many: Clinician <-> Patient
            builder.Entity<ClinicianPatientMap>()
                .HasKey(cpm => new { cpm.ClinicianUserId, cpm.PatientUserId });

            builder.Entity<ClinicianPatientMap>()
                .HasOne(cpm => cpm.ClinicianUser)
                .WithMany(u => u.AssignedPatients)
                .HasForeignKey(cpm => cpm.ClinicianUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete

            builder.Entity<ClinicianPatientMap>()
                .HasOne(cpm => cpm.PatientUser)
                .WithMany(u => u.AssignedClinicians)
                .HasForeignKey(cpm => cpm.PatientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Configure One-to-One: Patient <-> Profile
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.PatientProfile)
                .WithOne(p => p.PatientUser)
                .HasForeignKey<PatientProfile>(p => p.PatientUserId);

            // 3. Configure Comments
            builder.Entity<Comment>()
                .HasOne(c => c.AuthorUser)
                .WithMany(u => u.AuthoredComments)
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Comment>()
                .HasOne(c => c.PatientUser)
                .WithMany(u => u.ReceivedComments)
                .HasForeignKey(c => c.PatientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}