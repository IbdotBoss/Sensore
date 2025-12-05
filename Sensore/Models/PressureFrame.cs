using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    public class PressureFrame
    {
        [Key]
        public long FrameId { get; set; }

        [ForeignKey("ApplicationUser")]
        public string PatientUserId { get; set; }
        public ApplicationUser PatientUser { get; set; }

        public DateTime Timestamp { get; set; }

        // Stored as JSON string "[ [0,1,2...], [3,4,5...] ]"
        public string PressureDataJson { get; set; }

        // --- Metrics (Calculated on Ingestion) ---
        public int PeakPressureIndex { get; set; }

        public double ContactAreaPercent { get; set; }

        // Stored as JSON "{ 'UpperRight': 20, 'LowerLeft': 15 }"
        public string ZonalContactAreaJson { get; set; }

        public bool IsAlertFlagged { get; set; }
    }
}