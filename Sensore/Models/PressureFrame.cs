using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    public class PressureFrame
    {
        [Key]
        public long FrameId { get; set; }

        [Required]
        public string PatientUserId { get; set; } // Foreign Key

        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser PatientUser { get; set; }

        public DateTime Timestamp { get; set; }

        [Column(TypeName = "nvarchar(max)")] // For JSON string
        public string PressureData { get; set; }

        [Column(TypeName = "nvarchar(max)")] // For JSON string
        public string ZonalContactArea { get; set; }

        public int PeakPressureIndex { get; set; }
        public double ContactAreaPercent { get; set; }
        public bool IsAlertFlagged { get; set; }
    }
}
