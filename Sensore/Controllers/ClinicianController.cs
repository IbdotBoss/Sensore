using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    // Handles all clinician-specific pages and actions.
    // Clinicians can view their assigned patients, monitor pressure data,
    // adjust alert thresholds, and communicate with patients.
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

  // Displays the clinician's main dashboard showing all assigned patients.
    // Each patient card shows their current risk score, alert status, and unread message count.
        // Supports optional search filtering by patient name or email.
        public async Task<IActionResult> Index(string? searchString)
        {
          var clinician = await _userManager.GetUserAsync(User);
         if (clinician == null) return NotFound();

            // Get patients assigned to this clinician
       var assignedPatientsQuery = _context.ClinicianPatientMaps
        .Where(map => map.ClinicianUserId == clinician.Id)
              .Select(map => map.PatientUser)
  .AsQueryable();

      // Filter by name or email if search provided
       if (!string.IsNullOrEmpty(searchString))
            {
        assignedPatientsQuery = assignedPatientsQuery
         .Where(p => (p.FullName != null && p.FullName.Contains(searchString)) ||
     (p.Email != null && p.Email.Contains(searchString)));
   }

            var assignedPatientIds = await assignedPatientsQuery.Select(p => p.Id).ToListAsync();
  var assignedPatients = await assignedPatientsQuery.ToListAsync();

            // Get the most recent pressure frame for each patient
       var latestFrames = await _context.PressureFrames
        .Where(f => assignedPatientIds.Contains(f.PatientUserId))
     .GroupBy(f => f.PatientUserId)
        .Select(g => g.OrderByDescending(f => f.Timestamp).FirstOrDefault())
   .ToListAsync();

            // Find patients who triggered alerts in the last 24 hours
        var cutoffTime = DateTime.UtcNow.AddHours(-24);
          var patientsWithAlerts = await _context.PressureFrames
        .Where(f => assignedPatientIds.Contains(f.PatientUserId)
           && f.IsAlertFlagged
      && f.Timestamp >= cutoffTime)
        .Select(f => f.PatientUserId)
  .Distinct()
           .ToListAsync();

            // Count messages for each patient
  var messageCounts = await _context.Comments
      .Where(c => assignedPatientIds.Contains(c.PatientUserId))
            .GroupBy(c => c.PatientUserId)
    .Select(g => new { PatientId = g.Key, Count = g.Count() })
       .ToDictionaryAsync(x => x.PatientId, x => x.Count);

    // Build the view model for each patient
   var viewModel = new List<PatientListItemViewModel>();

   foreach (var patient in assignedPatients)
            {
    var lastFrame = latestFrames.FirstOrDefault(f => f?.PatientUserId == patient.Id);
         bool hasAlert = patientsWithAlerts.Contains(patient.Id);
        int msgCount = messageCounts.GetValueOrDefault(patient.Id, 0);

    // Risk score is 0-10 based on peak pressure (255 max = 10 risk)
          double riskScore = 0;
                if (lastFrame != null)
                {
         riskScore = Math.Min(Math.Round((double)lastFrame.PeakPressureIndex / 25.5, 1), 10.0);
  }

     viewModel.Add(new PatientListItemViewModel
       {
     PatientId = patient.Id,
        Name = patient.FullName ?? patient.UserName ?? "Unknown",
           Email = patient.Email ?? "No email",
              HasActiveAlert = hasAlert,
   LastUpdate = lastFrame?.Timestamp ?? DateTime.UtcNow,
        RiskScore = riskScore,
                MessageCount = msgCount
             });
            }

    ViewBag.SearchString = searchString;
            return View(viewModel);
        }

        // Shows the detailed view for a specific patient.
        // Includes live pressure heatmap, trend charts, threshold settings,
        // and the full communication history between clinician and patient.
        // Only accessible if this clinician is assigned to the patient.
public async Task<IActionResult> PatientDetail(string id)
   {
     if (string.IsNullOrEmpty(id)) return NotFound();

            // Verify this clinician is assigned to view this patient
  var clinician = await _userManager.GetUserAsync(User);
if (clinician == null) return NotFound();

    bool isAssigned = await _context.ClinicianPatientMaps
        .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == id);

     if (!isAssigned) return Forbid();

          var patient = await _context.Users.FindAsync(id);
            if (patient == null) return NotFound();

            // Get or create the patient's clinical profile
         var profile = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.PatientUserId == id);

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

      // Get the most recent frame for the heatmap display
            var latestFrame = await _context.PressureFrames
          .Where(f => f.PatientUserId == id)
    .OrderByDescending(f => f.Timestamp)
.FirstOrDefaultAsync();

 // Get recent frames for the trend chart (last 100)
   var history = await _context.PressureFrames
                .Where(f => f.PatientUserId == id)
              .OrderByDescending(f => f.Timestamp)
   .Take(100)
         .OrderBy(f => f.Timestamp)
      .ToListAsync();

            // Load comments with their replies for the communication panel
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

   // Saves updated alert threshold settings for a patient.
        // Clinicians can adjust when alerts trigger based on pressure levels
  // and minimum area size to reduce false positives from sensor noise.
        [HttpPost]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(int profileId, int highPressureThreshold, int minAlertArea)
  {
 var profile = await _context.PatientProfiles.FindAsync(profileId);
     if (profile == null) return NotFound();

       // Verify this clinician can modify this patient's settings
         var clinician = await _userManager.GetUserAsync(User);
  if (clinician == null) return NotFound();

      bool isAssigned = await _context.ClinicianPatientMaps
            .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == profile.PatientUserId);

 if (!isAssigned) return Forbid();

     // Clamp values to valid ranges
 highPressureThreshold = Math.Clamp(highPressureThreshold, 1, 255);
    minAlertArea = Math.Clamp(minAlertArea, 1, 1024);

     profile.HighPressureThreshold = highPressureThreshold;
    profile.MinAlertArea = minAlertArea;

            _context.Update(profile);
            await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = "Settings updated successfully.";
            return RedirectToAction("PatientDetail", new { id = profile.PatientUserId });
        }

  // Posts a new message or reply on a patient's communication thread.
        // Messages are visible to both the patient and their assigned clinicians.
        // If parentId is provided, the message becomes a reply to that comment.
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

    // Verify this clinician can comment on this patient
  bool isAssigned = await _context.ClinicianPatientMaps
         .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == patientId);

            if (!isAssigned) return Forbid();

    // If replying, verify the parent comment exists
       if (parentId.HasValue)
            {
     var parentExists = await _context.Comments
         .AnyAsync(c => c.CommentId == parentId.Value && c.PatientUserId == patientId);

   if (!parentExists)
    {
             parentId = null;
       }
         }

      var comment = new Comment
     {
    AuthorUserId = clinician.Id,
         PatientUserId = patientId,
    CommentText = commentText.Trim(),
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
