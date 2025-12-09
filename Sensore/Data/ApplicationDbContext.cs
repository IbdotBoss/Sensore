using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sensore.Models;

namespace Sensore.Data
{
    // Database context for the Sensore application.
    // Extends IdentityDbContext to include ASP.NET Core Identity tables.
    // Configures relationships between users, profiles, pressure data, and comments.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ========================================================================
        // DbSets - Define tables in the database
        // ========================================================================

        // Patient profiles containing clinical settings and thresholds.
        public DbSet<PatientProfile> PatientProfiles { get; set; }

        // Pressure sensor data frames from patient monitoring.
        public DbSet<PressureFrame> PressureFrames { get; set; }

        // Comments for patient-clinician communication.
        public DbSet<Comment> Comments { get; set; }

        // Mapping table for clinician-patient assignments.
        public DbSet<ClinicianPatientMap> ClinicianPatientMaps { get; set; }

        // Configures entity relationships and constraints.
        // Called when the model is being created.
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Initialize Identity tables (Users, Roles, etc.)
            base.OnModelCreating(builder);

            // ----------------------------------------------------------------
            // CLINICIAN-PATIENT MANY-TO-MANY RELATIONSHIP
            // A clinician can have multiple patients
            // A patient can have multiple clinicians
            // ----------------------------------------------------------------
            builder.Entity<ClinicianPatientMap>()
                .HasKey(cpm => new { cpm.ClinicianUserId, cpm.PatientUserId });

            builder.Entity<ClinicianPatientMap>()
                .HasOne(cpm => cpm.ClinicianUser)
                .WithMany(u => u.AssignedPatients)
                .HasForeignKey(cpm => cpm.ClinicianUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            builder.Entity<ClinicianPatientMap>()
                .HasOne(cpm => cpm.PatientUser)
                .WithMany(u => u.AssignedClinicians)
                .HasForeignKey(cpm => cpm.PatientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ----------------------------------------------------------------
            // PATIENT-PROFILE ONE-TO-ONE RELATIONSHIP
            // Each patient has exactly one profile
            // ----------------------------------------------------------------
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.PatientProfile)
                .WithOne(p => p.PatientUser)
                .HasForeignKey<PatientProfile>(p => p.PatientUserId);

            // ----------------------------------------------------------------
            // COMMENT RELATIONSHIPS
            // Comments have an author (who wrote it) and a patient (who it's about)
            // Both use Restrict delete to prevent accidental data loss
            // ----------------------------------------------------------------
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