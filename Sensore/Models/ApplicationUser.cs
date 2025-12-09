using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Represents a user in the Sensore system.
    // Extends IdentityUser to support custom properties and relationships.
    // Users can be Admins, Clinicians, or Patients.
    public class ApplicationUser : IdentityUser
    {
        // The user's full name for display purposes.
        // Marked as personal data for GDPR compliance.
        [PersonalData]
        public string FullName { get; set; }

     // Stores the user's role type as a string: "Admin", "Clinician", or "Patient".
      // Used for quick role identification without querying the roles table.
        public string RoleType { get; set; }

        // ========================================================================
        // NAVIGATION PROPERTIES
// Define relationships between users and other entities
  // ========================================================================

    // The patient's clinical profile with pressure monitoring settings.
   // Only applicable for users with the Patient role.
        public PatientProfile PatientProfile { get; set; }

  // Collection of pressure sensor data frames recorded for this patient.
        // Only applicable for users with the Patient role.
        public ICollection<PressureFrame> PressureFrames { get; set; }

        // Comments created by this user (as author).
        // Clinicians and Patients can author comments.
        [InverseProperty("AuthorUser")]
        public ICollection<Comment> AuthoredComments { get; set; }

        // Comments made about this user (as patient).
        // Only applicable for users with the Patient role.
   [InverseProperty("PatientUser")]
        public ICollection<Comment> ReceivedComments { get; set; }

// Patients assigned to this clinician.
        // Only applicable for users with the Clinician role.
  [InverseProperty("ClinicianUser")]
      public ICollection<ClinicianPatientMap> AssignedPatients { get; set; }

        // Clinicians assigned to this patient.
        // Only applicable for users with the Patient role.
        [InverseProperty("PatientUser")]
        public ICollection<ClinicianPatientMap> AssignedClinicians { get; set; }
    }
}