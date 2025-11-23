using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sensore.Models;

namespace Sensore.Data
{
    /// <summary>
    /// Database context for the Sensore application, including Identity tables
    /// and custom application tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

     // DbSets for custom application tables
public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<PressureFrame> PressureFrames { get; set; }
      public DbSet<Comment> Comments { get; set; }
    public DbSet<ClinicianPatientMap> ClinicianPatientMaps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          base.OnModelCreating(modelBuilder);

            // Configure composite primary key for ClinicianPatientMap
          modelBuilder.Entity<ClinicianPatientMap>()
      .HasKey(cpm => new { cpm.ClinicianUserId, cpm.PatientUserId });

            // Configure the many-to-many relationship between clinicians and patients
            modelBuilder.Entity<ClinicianPatientMap>()
             .HasOne(cpm => cpm.ClinicianUser)
       .WithMany(u => u.PatientsAssigned)
      .HasForeignKey(cpm => cpm.ClinicianUserId)
         .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClinicianPatientMap>()
        .HasOne(cpm => cpm.PatientUser)
      .WithMany(u => u.CliniciansAssigned)
         .HasForeignKey(cpm => cpm.PatientUserId)
      .OnDelete(DeleteBehavior.Restrict);

            // Configure Comment relationships
      modelBuilder.Entity<Comment>()
     .HasOne(c => c.AuthorUser)
    .WithMany(u => u.AuthoredComments)
             .HasForeignKey(c => c.AuthorUserId)
    .OnDelete(DeleteBehavior.Restrict);

   modelBuilder.Entity<Comment>()
    .HasOne(c => c.PatientUser)
   .WithMany(u => u.SubjectComments)
        .HasForeignKey(c => c.PatientUserId)
   .OnDelete(DeleteBehavior.Restrict);

   // Configure self-referencing relationship for Comment replies
            modelBuilder.Entity<Comment>()
        .HasOne(c => c.ParentComment)
       .WithMany(c => c.Replies)
      .HasForeignKey(c => c.ParentCommentId)
           .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
