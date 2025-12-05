using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    [Authorize(Roles = "Clinician")]
    public class ClinicianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClinicianController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Dashboard: List of My Patients with Search and Enhanced Metrics
        public async Task<IActionResult> Index(string? searchString)
        {
            var clinician = await _userManager.GetUserAsync(User);
            if (clinician == null) return NotFound();

            // Fetch assigned patients with optional search filter
            var assignedPatientsQuery = _context.ClinicianPatientMaps
                .Where(map => map.ClinicianUserId == clinician.Id)
                .Select(map => map.PatientUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                assignedPatientsQuery = assignedPatientsQuery
                    .Where(p => (p.FullName != null && p.FullName.Contains(searchString)) || 
                               (p.Email != null && p.Email.Contains(searchString)));
            }

            var assignedPatients = await assignedPatientsQuery.ToListAsync();
            var viewModel = new List<PatientListItemViewModel>();

            foreach (var patient in assignedPatients)
            {
                // Check for alerts in the last 24 hours
                bool hasAlert = await _context.PressureFrames
                    .AnyAsync(f => f.PatientUserId == patient.Id
                                   && f.IsAlertFlagged
                                   && f.Timestamp >= DateTime.UtcNow.AddHours(-24));

                var lastFrame = await _context.PressureFrames
                    .Where(f => f.PatientUserId == patient.Id)
                    .OrderByDescending(f => f.Timestamp)
                    .FirstOrDefaultAsync();

                // Count messages
                int msgCount = await _context.Comments
                    .CountAsync(c => c.PatientUserId == patient.Id);

                // Calculate Risk Score (0-10) based on peak pressure
                double riskScore = 0;
                if (lastFrame != null)
                {
                    riskScore = Math.Round((double)lastFrame.PeakPressureIndex / 25.5, 1); // Map 255 -> 10
                }

                viewModel.Add(new PatientListItemViewModel
                {
                    PatientId = patient.Id,
                    Name = patient.FullName ?? patient.UserName ?? "Unknown",
                    Email = patient.Email ?? "No email",
                    HasActiveAlert = hasAlert,
                    LastUpdate = lastFrame?.Timestamp ?? DateTime.MinValue,
                    RiskScore = riskScore,
                    MessageCount = msgCount
                });
            }

            ViewBag.SearchString = searchString;
            return View(viewModel);
        }

        // 2. Patient Detail View
        public async Task<IActionResult> PatientDetail(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Security Check: Ensure this patient is actually assigned to this clinician
            var clinician = await _userManager.GetUserAsync(User);
            if (clinician == null) return NotFound();

            bool isAssigned = await _context.ClinicianPatientMaps
                .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == id);

            if (!isAssigned) return Forbid();

            var patient = await _context.Users.FindAsync(id);
            if (patient == null) return NotFound();

            var profile = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.PatientUserId == id);

            // Create default profile if missing
            if (profile == null)
            {
                profile = new PatientProfile 
                { 
                    PatientUserId = id,
                    HighPressureThreshold = 150,
                    MinAlertArea = 10,
                    ContactThreshold = 3
                };
                _context.PatientProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            var latestFrame = await _context.PressureFrames
                .Where(f => f.PatientUserId == id)
                .OrderByDescending(f => f.Timestamp)
                .FirstOrDefaultAsync();

            var history = await _context.PressureFrames
                .Where(f => f.PatientUserId == id && f.Timestamp >= DateTime.UtcNow.AddHours(-24))
                .OrderBy(f => f.Timestamp)
                .ToListAsync();

            var recentComments = await _context.Comments
                .Include(c => c.AuthorUser)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.AuthorUser)
                .Where(c => c.PatientUserId == id && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .ToListAsync();

            var viewModel = new ClinicianPatientDetailViewModel
            {
                Patient = patient,
                Profile = profile,
                LatestFrame = latestFrame,
                History = history,
                RecentComments = recentComments
            };

            return View(viewModel);
        }

        // 3. Update Thresholds (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(int profileId, int highPressureThreshold, int minAlertArea)
        {
            var profile = await _context.PatientProfiles.FindAsync(profileId);
            if (profile == null) return NotFound();

            // Security check: Verify clinician has access to this patient
            var clinician = await _userManager.GetUserAsync(User);
            if (clinician == null) return NotFound();

            bool isAssigned = await _context.ClinicianPatientMaps
                .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == profile.PatientUserId);

            if (!isAssigned) return Forbid();

            profile.HighPressureThreshold = highPressureThreshold;
            profile.MinAlertArea = minAlertArea;

            _context.Update(profile);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Settings updated successfully.";
            return RedirectToAction("PatientDetail", new { id = profile.PatientUserId });
        }

        // 4. Clinician Reply to Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToComment(string patientId, string commentText, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(commentText) || string.IsNullOrEmpty(patientId))
            {
                TempData["ErrorMessage"] = "Comment text and patient ID are required.";
                return RedirectToAction("PatientDetail", new { id = patientId });
            }

            var clinician = await _userManager.GetUserAsync(User);
            if (clinician == null) return NotFound();

            // Security check: Verify clinician has access to this patient
            bool isAssigned = await _context.ClinicianPatientMaps
                .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == patientId);

            if (!isAssigned) return Forbid();

            var comment = new Comment
            {
                AuthorUserId = clinician.Id,
                PatientUserId = patientId,
                CommentText = commentText,
                CreatedAt = DateTime.UtcNow,
                ThreadTimestamp = DateTime.UtcNow,
                ParentCommentId = parentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment posted successfully.";
            return RedirectToAction("PatientDetail", new { id = patientId });
        }
    }
}