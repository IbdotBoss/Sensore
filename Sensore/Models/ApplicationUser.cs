using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Extends IdentityUser with custom properties for the Sensore system.
    public class ApplicationUser : IdentityUser
    {
        // Display name for the user (e.g., "Dr. Ibrahim Uthman")
        [PersonalData]
   [StringLength(100)]
     public string? FullName { get; set; }

        // Quick role lookup without querying AspNetUserRoles table
  [StringLength(20)]
        public string? RoleType { get; set; }

     // Patient's alert threshold settings (null for non-patients)
        public virtual PatientProfile? PatientProfile { get; set; }

        // Pressure sensor readings for this patient
      public virtual ICollection<PressureFrame> PressureFrames { get; set; } = new List<PressureFrame>();

        // Comments written by this user
 [InverseProperty("AuthorUser")]
        public virtual ICollection<Comment> AuthoredComments { get; set; } = new List<Comment>();

        // Comments on this patient's record
        [InverseProperty("PatientUser")]
     public virtual ICollection<Comment> ReceivedComments { get; set; } = new List<Comment>();

        // Patients assigned to this clinician
        [InverseProperty("ClinicianUser")]
        public virtual ICollection<ClinicianPatientMap> AssignedPatients { get; set; } = new List<ClinicianPatientMap>();

        // Clinicians assigned to this patient
        [InverseProperty("PatientUser")]
        public virtual ICollection<ClinicianPatientMap> AssignedClinicians { get; set; } = new List<ClinicianPatientMap>();
    }
}