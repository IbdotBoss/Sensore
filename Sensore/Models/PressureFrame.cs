using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Single frame of 32x32 pressure sensor data with pre-calculated metrics.
    public class PressureFrame
    {
        [Key]
        public long FrameId { get; set; }

        // Patient this frame belongs to
        [Required]
        [ForeignKey("PatientUser")]
        public string PatientUserId { get; set; } = string.Empty;

        public virtual ApplicationUser? PatientUser { get; set; }

        // When this reading was captured
        [Required]
        public DateTime Timestamp { get; set; }

        // 32x32 matrix stored as JSON array
        [Required]
        public string PressureDataJson { get; set; } = "[]";

        // Highest pressure in valid blobs (pre-calculated for dashboard)
        [Range(0, 255)]
        public int PeakPressureIndex { get; set; }

        // Percentage of sensor showing contact
        [Range(0.0, 100.0)]
        public double ContactAreaPercent { get; set; }

        // Contact area broken down by zones (JSON)
        public string? ZonalContactAreaJson { get; set; }

        // True if this frame triggered a high-pressure alert
        public bool IsAlertFlagged { get; set; }
    }
}