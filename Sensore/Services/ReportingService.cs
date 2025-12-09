using Microsoft.EntityFrameworkCore;
using Sensore.Data;

namespace Sensore.Services
{
    // Service for generating patient reports and comparisons.
    // Provides daily pressure trend analysis for patient feedback.
    public class ReportingService
    {
        private readonly ApplicationDbContext _context;

        public ReportingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Generates a daily comparison report for a patient.
        // Compares today's average pressure with yesterday's average.
        // Used to provide motivational feedback on the patient dashboard.
        // param: patientId - The patient's user ID
        // returns: A human-readable comparison message
        public async Task<string> GetDailyComparison(string patientId)
        {
            var now = DateTime.Now;
            var yesterday = now.AddDays(-1);

            // Get today's peak pressure values (last 24 hours)
            var todayFrames = await _context.PressureFrames
                .Where(f => f.PatientUserId == patientId && f.Timestamp >= yesterday)
                .Select(f => f.PeakPressureIndex)
                .ToListAsync();

            // Get yesterday's peak pressure values (24-48 hours ago)
            var previousFrames = await _context.PressureFrames
                .Where(f => f.PatientUserId == patientId && f.Timestamp >= yesterday.AddDays(-1) && f.Timestamp < yesterday)
                .Select(f => f.PeakPressureIndex)
                .ToListAsync();

            // Need data from both days to make a comparison
            if (!todayFrames.Any() || !previousFrames.Any())
            {
                return "Not enough data for comparison.";
            }

            // Calculate averages and compare
            double todayAvg = todayFrames.Average();
            double prevAvg = previousFrames.Average();

            double diff = todayAvg - prevAvg;
            double percentChange = (diff / prevAvg) * 100;

            // Generate appropriate feedback message
            if (Math.Abs(percentChange) < 1)
            {
                // Less than 1% change = stable
                return "Your pressure levels are stable compared to yesterday.";
            }
            else if (percentChange < 0)
            {
                // Negative change = improvement (lower pressure is better)
                return $"Great job! Your average pressure is {Math.Abs(percentChange):F1}% lower than yesterday.";
            }
            else
            {
                // Positive change = higher pressure (needs attention)
                return $"Attention: Your average pressure is {percentChange:F1}% higher than yesterday.";
            }
        }
    }
}