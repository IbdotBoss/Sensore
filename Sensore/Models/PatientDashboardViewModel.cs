using Sensore.Models;

namespace Sensore.Models
{
    // View model for the Patient Dashboard.
    // Aggregates all data needed for the patient's main view including
    // pressure data, profile settings, comments, and daily reports.
    public class PatientDashboardViewModel
    {
        // The most recent pressure frame for heatmap visualization.
        // May be null if no data has been recorded yet.
        public PressureFrame LatestFrame { get; set; }

        // Historical pressure frames for trend chart display.
        // Typically contains the last 24 hours or 100 most recent frames.
        public List<PressureFrame> History { get; set; }

        // The patient's clinical profile with alert thresholds.
        // Used to display current settings and alert levels.
        public PatientProfile Profile { get; set; }

        // The patient's display name for the dashboard greeting.
        public string UserName { get; set; }
        
        // Recent comments and replies from the care team.
        // Enables patient-clinician communication.
        public List<Comment> RecentComments { get; set; } = new List<Comment>();

        // Daily comparison report showing pressure trends.
        // Provides feedback like "Your pressure is X% lower than yesterday."
        public string DailyComparisonReport { get; set; }
    }
}