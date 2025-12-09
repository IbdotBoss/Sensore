using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Represents a single frame of pressure sensor data captured from a patient.
    // Each frame contains a 32x32 matrix of pressure values and calculated metrics.
    // Frames are recorded at regular intervals and stored for analysis and visualization.
    public class PressureFrame
    {
        // Unique identifier for the pressure frame.
  // Uses long type to support large datasets over time.
        [Key]
    public long FrameId { get; set; }

        // Foreign key to the patient this frame belongs to.
      [ForeignKey("ApplicationUser")]
        public string PatientUserId { get; set; }

  // Navigation property to the patient user.
        public ApplicationUser PatientUser { get; set; }

        // When this pressure frame was recorded.
        // Used for historical analysis and trend tracking.
     public DateTime Timestamp { get; set; }

        // The raw pressure data stored as a JSON 2D array.
        // Format: "[ [0,1,2...], [3,4,5...], ... ]" (32x32 matrix)
        // Values range from 0-255 representing pressure intensity.
  public string PressureDataJson { get; set; }

        // ========================================================================
      // CALCULATED METRICS
        // Pre-computed during data ingestion for faster dashboard display
   // ========================================================================

        // The highest pressure value found in valid pressure blobs.
        // Range: 0-255. Used for trend charts and alert detection.
        public int PeakPressureIndex { get; set; }

        // Percentage of sensor area showing contact with the patient.
        // Calculated as: (pixels above threshold / total pixels) * 100
        public double ContactAreaPercent { get; set; }

   // Contact area broken down by zones (JSON format).
        // Example: "{ 'UpperRight': 20, 'LowerLeft': 15 }"
      // Used for detailed pressure distribution analysis.
        public string ZonalContactAreaJson { get; set; }

     // Whether this frame triggered a high-pressure alert.
        // True if peak pressure exceeds the patient's threshold in a valid blob.
        public bool IsAlertFlagged { get; set; }
    }
}