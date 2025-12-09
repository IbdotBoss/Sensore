using Sensore.Models;

namespace Sensore.Models
{
    // View model for a single patient in the clinician's patient list.
    // Shows summary information and alert status for quick triage.
    public class PatientListItemViewModel
    {
        // The unique identifier of the patient.
     public string PatientId { get; set; }

        // The patient's display name.
        public string Name { get; set; }

        // The patient's email address.
        public string Email { get; set; }

   // Indicates if the patient has had a pressure alert in the last 24 hours.
        // Used to highlight patients needing attention.
        public bool HasActiveAlert { get; set; }

        // Timestamp of the patient's most recent pressure reading.
    public DateTime LastUpdate { get; set; }

      // Calculated risk score from 0-10 based on peak pressure.
        // Higher scores indicate higher pressure levels.
     public double RiskScore { get; set; }

        // Total number of comments/messages for this patient.
        // Indicates communication activity level.
   public int MessageCount { get; set; }
    }

    // View model for the clinician's detailed patient view.
    // Contains all information needed for patient monitoring and settings adjustment.
    public class ClinicianPatientDetailViewModel
    {
        // The patient user being viewed.
        public ApplicationUser Patient { get; set; } = null!;

        // The patient's clinical profile with editable alert thresholds.
        public PatientProfile Profile { get; set; } = null!;

        // The most recent pressure frame for heatmap display.
        // May be null if no data exists.
        public PressureFrame? LatestFrame { get; set; }

        // Historical pressure frames for trend analysis.
        public List<PressureFrame> History { get; set; } = new List<PressureFrame>();

  // Recent comments from both patient and clinician.
   // Enables threaded communication view.
        public List<Comment> RecentComments { get; set; } = new List<Comment>();
    }
}