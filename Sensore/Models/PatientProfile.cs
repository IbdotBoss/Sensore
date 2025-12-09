using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Represents a patient's clinical profile and pressure monitoring settings.
    // Each patient has one profile that stores their personalized alert thresholds.
    // Clinicians can adjust these settings based on patient needs.
    public class PatientProfile
    {
      // Unique identifier for the profile.
        [Key]
     public int ProfileId { get; set; }

        // Foreign key to the patient user.
        [ForeignKey("ApplicationUser")]
   public string PatientUserId { get; set; }

   // Navigation property to the patient user.
        public ApplicationUser PatientUser { get; set; }

        // ========================================================================
        // CLINICAL CONFIGURATION
        // Alert thresholds customizable per patient by clinicians
        // ========================================================================

        // Pressure value (0-255) above which an alert is triggered.
        // Higher values require more pressure to trigger alerts.
      // Default: 150
        public int HighPressureThreshold { get; set; } = 150;

     // Minimum number of connected pixels to consider as a valid pressure area.
   // Smaller blobs are ignored to reduce noise.
        // Default: 10 pixels
        public int MinAlertArea { get; set; } = 10;

        // Minimum pressure value to consider as "contact" with the sensor.
     // Values below this are treated as no contact.
        // Default: 3
        public int ContactThreshold { get; set; } = 3;
    }
}