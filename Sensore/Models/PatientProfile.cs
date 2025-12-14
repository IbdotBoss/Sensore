using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Stores customizable alert thresholds for each patient.
    public class PatientProfile
    {
        [Key]
        public int ProfileId { get; set; }

        // Links to the patient user (1:1 relationship)
        [Required]
        [ForeignKey("PatientUser")]
        public string PatientUserId { get; set; } = string.Empty;

        public virtual ApplicationUser? PatientUser { get; set; }

        // Pressure level that triggers an alert (0-255)
        [Range(1, 255)]
        public int HighPressureThreshold { get; set; } = 150;

        // Minimum blob size to consider as valid pressure area
        [Range(1, 1024)]
        public int MinAlertArea { get; set; } = 10;

        // Minimum pressure to register as contact with sensor
        [Range(0, 50)]
        public int ContactThreshold { get; set; } = 3;
    }
}