using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;
using Sensore.Services;

namespace Sensore.Controllers
{
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

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 1. Get the Profile (for thresholds)
            var profile = await _context.PatientProfiles
                                        .FirstOrDefaultAsync(p => p.PatientUserId == user.Id);

            // 2. Get the Latest Frame (For Heatmap)
            var latestFrame = await _context.PressureFrames
                                            .Where(f => f.PatientUserId == user.Id)
                                            .OrderByDescending(f => f.Timestamp)
                                            .FirstOrDefaultAsync();

            // 3. Get History (Last 24h for Graph)
            var history = await _context.PressureFrames
                                        .Where(f => f.PatientUserId == user.Id && f.Timestamp >= DateTime.UtcNow.AddHours(-24))
                                        .OrderBy(f => f.Timestamp)
                                        .ToListAsync();

            // 4. Get Recent Comments (including clinician replies)
            var recentComments = await _context.Comments
                                        .Include(c => c.AuthorUser)
                                        .Include(c => c.Replies)
                                            .ThenInclude(r => r.AuthorUser)
                                        .Where(c => c.PatientUserId == user.Id && c.ParentCommentId == null)
                                        .OrderByDescending(c => c.CreatedAt)
                                        .Take(10)
                                        .ToListAsync();

            // 5. Get Daily Comparison Report
            var dailyReport = await _reportingService.GetDailyComparison(user.Id);

            var viewModel = new PatientDashboardViewModel
            {
                LatestFrame = latestFrame ?? new PressureFrame(), // Ensure non-null assignment
                History = history,
                Profile = profile,
                UserName = user.FullName ?? user.UserName,
                RecentComments = recentComments,
                DailyComparisonReport = dailyReport
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string commentText)
        {
            var user = await _userManager.GetUserAsync(User);
            if (string.IsNullOrWhiteSpace(commentText)) return RedirectToAction("Dashboard");

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