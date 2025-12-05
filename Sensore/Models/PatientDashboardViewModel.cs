using Sensore.Models;

namespace Sensore.Models
{
    public class PatientDashboardViewModel
    {
        public PressureFrame LatestFrame { get; set; }
        public List<PressureFrame> History { get; set; }
        public PatientProfile Profile { get; set; }
        public string UserName { get; set; }
        
        // New properties for feedback loop and reporting
        public List<Comment> RecentComments { get; set; } = new List<Comment>();
        public string DailyComparisonReport { get; set; }
    }
}