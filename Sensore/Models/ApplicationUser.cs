using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string FullName { get; set; }

        // Helper to store "Admin", "Clinician", or "Patient" string
        public string RoleType { get; set; }

        // --- Navigation Properties ---

        public PatientProfile PatientProfile { get; set; }

        public ICollection<PressureFrame> PressureFrames { get; set; }

        [InverseProperty("AuthorUser")]
        public ICollection<Comment> AuthoredComments { get; set; }

        [InverseProperty("PatientUser")]
        public ICollection<Comment> ReceivedComments { get; set; }

        // Many-to-Many Relationships
        [InverseProperty("ClinicianUser")]
        public ICollection<ClinicianPatientMap> AssignedPatients { get; set; }

        [InverseProperty("PatientUser")]
        public ICollection<ClinicianPatientMap> AssignedClinicians { get; set; }
    }
}