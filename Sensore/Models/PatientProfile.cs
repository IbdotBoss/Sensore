using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    public class PatientProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [Required]
        public string PatientUserId { get; set; } // Foreign Key

        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser PatientUser { get; set; }

        public int HighPressureThreshold { get; set; }
        public int MinAlertArea { get; set; }
        public int ContactThreshold { get; set; }
    }
}
