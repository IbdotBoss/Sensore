using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Junction table for M:N clinician-patient assignments.
    public class ClinicianPatientMap
    {
        [Required]
        [ForeignKey("ClinicianUser")]
        public string ClinicianUserId { get; set; } = string.Empty;

        public virtual ApplicationUser? ClinicianUser { get; set; }

        [Required]
        [ForeignKey("PatientUser")]
        public string PatientUserId { get; set; } = string.Empty;

        public virtual ApplicationUser? PatientUser { get; set; }
    }
}