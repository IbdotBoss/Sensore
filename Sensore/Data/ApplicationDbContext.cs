using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sensore.Models;

namespace Sensore.Data
{
    // Database context extending IdentityDbContext for user management.
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
    base.OnModelCreating(builder);

  // Clinician-Patient M:N with composite key
        builder.Entity<ClinicianPatientMap>()
             .HasKey(cpm => new { cpm.ClinicianUserId, cpm.PatientUserId });

     builder.Entity<ClinicianPatientMap>()
       .HasOne(cpm => cpm.ClinicianUser)
   .WithMany(u => u.AssignedPatients)
   .HasForeignKey(cpm => cpm.ClinicianUserId)
          .OnDelete(DeleteBehavior.Restrict);

       builder.Entity<ClinicianPatientMap>()
     .HasOne(cpm => cpm.PatientUser)
                .WithMany(u => u.AssignedClinicians)
     .HasForeignKey(cpm => cpm.PatientUserId)
 .OnDelete(DeleteBehavior.Restrict);

          // Patient-Profile 1:1
      builder.Entity<ApplicationUser>()
   .HasOne(u => u.PatientProfile)
  .WithOne(p => p.PatientUser)
             .HasForeignKey<PatientProfile>(p => p.PatientUserId)
    .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PatientProfile>()
.HasIndex(p => p.PatientUserId)
        .IsUnique();

         // Pressure frames - cascade delete with patient
            builder.Entity<PressureFrame>()
.HasOne(f => f.PatientUser)
   .WithMany(u => u.PressureFrames)
      .HasForeignKey(f => f.PatientUserId)
     .OnDelete(DeleteBehavior.Cascade);

     builder.Entity<PressureFrame>()
     .HasIndex(f => new { f.PatientUserId, f.Timestamp });

     // Comments - restrict delete to preserve history
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

            // Comment replies - orphan on parent delete
     builder.Entity<Comment>()
           .HasOne(c => c.ParentComment)
    .WithMany(c => c.Replies)
        .HasForeignKey(c => c.ParentCommentId)
             .OnDelete(DeleteBehavior.SetNull);

  builder.Entity<Comment>()
            .HasIndex(c => c.PatientUserId);
      }
    }
}