using Sensore.Models;

namespace Sensore.Models
{
    public class PatientListItemViewModel
    {
        public string PatientId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool HasActiveAlert { get; set; } // True if alert in last 24h
        public DateTime LastUpdate { get; set; }

        // ✅ Added these two missing properties:
        public double RiskScore { get; set; }      // Error 1 - Fixed
        public int MessageCount { get; set; }       // Error 2 - Fixed
    }

    public class ClinicianPatientDetailViewModel
    {
        public ApplicationUser Patient { get; set; } = null!;
        public PatientProfile Profile { get; set; } = null!;
        public PressureFrame? LatestFrame { get; set; }
        public List<PressureFrame> History { get; set; } = new List<PressureFrame>();
        public List<Comment> RecentComments { get; set; } = new List<Comment>();
    }
}