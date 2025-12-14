using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;
using Sensore.Services;

namespace Sensore.Controllers
{
    // Handles the patient-facing pages of the application.
    // Patients can view their pressure data, see trends over time,
    // and communicate with their assigned clinicians.
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

        // Shows the patient's main dashboard with all their health data.
        // Displays a live pressure heatmap, historical trend charts,
        // recent messages from clinicians, and a daily progress report.
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.PatientProfiles
                                        .FirstOrDefaultAsync(p => p.PatientUserId == user.Id);

            // Get the most recent pressure reading for the heatmap
            var latestFrame = await _context.PressureFrames
                                            .Where(f => f.PatientUserId == user.Id)
                                            .OrderByDescending(f => f.Timestamp)
                                            .FirstOrDefaultAsync();

            // Get recent frames for the trend chart (last 100 readings)
            var history = await _context.PressureFrames
                                        .Where(f => f.PatientUserId == user.Id)
                                        .OrderByDescending(f => f.Timestamp)
                                        .Take(100)
                                        .OrderBy(f => f.Timestamp)
                                        .ToListAsync();

            // Load comments with clinician replies for the chat panel
            var recentComments = await _context.Comments
                                        .Include(c => c.AuthorUser)
                                        .Include(c => c.Replies)
                                            .ThenInclude(r => r.AuthorUser)
                                        .Where(c => c.PatientUserId == user.Id && c.ParentCommentId == null)
                                        .OrderByDescending(c => c.CreatedAt)
                                        .Take(10)
                                        .ToListAsync();

            // Generate a comparison with yesterday's data
            var dailyReport = await _reportingService.GetDailyComparison(user.Id);

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

        // Sends a message from the patient to their care team.
        // The message will appear in the clinician's communication panel
        // and can be replied to by any clinician assigned to this patient.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string commentText)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Dashboard");
            
            if (string.IsNullOrWhiteSpace(commentText)) 
            {
                TempData["ErrorMessage"] = "Comment text cannot be empty.";
                return RedirectToAction("Dashboard");
            }

            var comment = new Comment
            {
                AuthorUserId = user.Id,
                PatientUserId = user.Id,
                CommentText = commentText,
                CreatedAt = DateTime.UtcNow,
                ThreadTimestamp = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment sent to your care team successfully.";
            return RedirectToAction("Dashboard");
        }
    }
}