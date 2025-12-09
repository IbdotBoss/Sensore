using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;
using Sensore.Services;

namespace Sensore.Controllers
{
    // Controller for patient-specific functionality.
    // Provides dashboard with pressure data, comments, and daily reports.
    // Only accessible by users with the Patient role.
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ReportingService _reportingService;

        public PatientController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ReportingService reportingService)
        {
            _context = context;
            _userManager = userManager;
            _reportingService = reportingService;
        }

        // Displays the patient's main dashboard with pressure data and communication.
        // Shows:
        // - Latest pressure heatmap
        // - Historical pressure trend chart
        // - Recent comments from care team
        // - Daily comparison report
        public async Task<IActionResult> Dashboard()
        {
            // Get the currently logged-in patient
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 1. Get the patient's profile (contains alert thresholds)
            var profile = await _context.PatientProfiles
                                        .FirstOrDefaultAsync(p => p.PatientUserId == user.Id);

            // 2. Get the latest pressure frame for heatmap visualization
            var latestFrame = await _context.PressureFrames
                                            .Where(f => f.PatientUserId == user.Id)
                                            .OrderByDescending(f => f.Timestamp)
                                            .FirstOrDefaultAsync();

            // 3. Get historical data for the trend chart
            // Load latest 100 records regardless of time to handle seeded historical data
            var history = await _context.PressureFrames
                                        .Where(f => f.PatientUserId == user.Id)
                                        .OrderByDescending(f => f.Timestamp)
                                        .Take(100)
                                        .OrderBy(f => f.Timestamp) // Re-order chronologically for chart display
                                        .ToListAsync();

            // 4. Get recent comments including clinician replies for communication panel
            var recentComments = await _context.Comments
                                        .Include(c => c.AuthorUser)
                                        .Include(c => c.Replies)
                                            .ThenInclude(r => r.AuthorUser)
                                        .Where(c => c.PatientUserId == user.Id && c.ParentCommentId == null)
                                        .OrderByDescending(c => c.CreatedAt)
                                        .Take(10)
                                        .ToListAsync();

            // 5. Generate daily comparison report for feedback
            var dailyReport = await _reportingService.GetDailyComparison(user.Id);

            // Build the view model with all dashboard data
            var viewModel = new PatientDashboardViewModel
            {
                LatestFrame = latestFrame,
                History = history,
                Profile = profile,
                UserName = user.FullName ?? user.UserName ?? "User",
                RecentComments = recentComments,
                DailyComparisonReport = dailyReport
            };

            return View(viewModel);
        }

        // Allows the patient to send a comment to their care team.
        // Comments appear in the clinician's view of this patient.
        // param: commentText - The text content of the comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string commentText)
        {
            // Get the currently logged-in patient
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Dashboard");
            
            // Validate comment text is not empty
            if (string.IsNullOrWhiteSpace(commentText)) 
            {
                TempData["ErrorMessage"] = "Comment text cannot be empty.";
                return RedirectToAction("Dashboard");
            }

            // Create a new top-level comment from the patient
            var comment = new Comment
            {
                AuthorUserId = user.Id,
                PatientUserId = user.Id,
                CommentText = commentText,
                CreatedAt = DateTime.UtcNow,
                ThreadTimestamp = DateTime.UtcNow
            };

            // Save the comment to the database
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment sent to your care team successfully.";
            return RedirectToAction("Dashboard");
        }
    }
}