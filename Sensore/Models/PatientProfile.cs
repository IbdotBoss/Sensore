using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    public class PatientProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [ForeignKey("ApplicationUser")]
        public string PatientUserId { get; set; }
        public ApplicationUser PatientUser { get; set; }

        // Clinical Configuration
        public int HighPressureThreshold { get; set; } = 150; // Configurable alert level
        public int MinAlertArea { get; set; } = 10;           // Ignore blobs smaller than this
        public int ContactThreshold { get; set; } = 3;        // Cutoff for "contact"
    }
}