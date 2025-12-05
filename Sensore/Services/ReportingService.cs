using Microsoft.EntityFrameworkCore;
using Sensore.Data;

namespace Sensore.Services
{
    public class ReportingService
    {
        private readonly ApplicationDbContext _context;

        public ReportingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetDailyComparison(string patientId)
        {
            var now = DateTime.Now;
            var yesterday = now.AddDays(-1);

            // Fetch Today's Data
            var todayFrames = await _context.PressureFrames
                .Where(f => f.PatientUserId == patientId && f.Timestamp >= yesterday)
                .Select(f => f.PeakPressureIndex)
                .ToListAsync();

            // Fetch Yesterday's Data (24h to 48h ago)
            var previousFrames = await _context.PressureFrames
                .Where(f => f.PatientUserId == patientId && f.Timestamp >= yesterday.AddDays(-1) && f.Timestamp < yesterday)
                .Select(f => f.PeakPressureIndex)
                .ToListAsync();

            if (!todayFrames.Any() || !previousFrames.Any())
            {
                return "Not enough data for comparison.";
            }

            double todayAvg = todayFrames.Average();
            double prevAvg = previousFrames.Average();

            double diff = todayAvg - prevAvg;
            double percentChange = (diff / prevAvg) * 100;

            if (Math.Abs(percentChange) < 1)
            {
                return "Your pressure levels are stable compared to yesterday.";
            }
            else if (percentChange < 0)
            {
                return $"Great job! Your average pressure is {Math.Abs(percentChange):F1}% lower than yesterday.";
            }
            else
            {
                return $"Attention: Your average pressure is {percentChange:F1}% higher than yesterday.";
            }
        }
    }
}