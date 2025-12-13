using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    // Controller for clinician-specific functionality.
    // Provides patient list, detailed patient views, and care team communication.
    // Only accessible by users with the Clinician role.
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

        // ========================================================================
        // PATIENT LIST (DASHBOARD)
        // ========================================================================

   // Displays the clinician's dashboard with a list of assigned patients.
        // Shows patient status, alert indicators, risk scores, and message counts.
     // Supports search by patient name or email.
        // param: searchString - Optional filter for patient name or email
        public async Task<IActionResult> Index(string? searchString)
        {
            // Get the currently logged-in clinician
         var clinician = await _userManager.GetUserAsync(User);
 if (clinician == null) return NotFound();

            // Query patients assigned to this clinician
            var assignedPatientsQuery = _context.ClinicianPatientMaps
              .Where(map => map.ClinicianUserId == clinician.Id)
     .Select(map => map.PatientUser)
      .AsQueryable();

        // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
   {
         assignedPatientsQuery = assignedPatientsQuery
      .Where(p => (p.FullName != null && p.FullName.Contains(searchString)) ||
     (p.Email != null && p.Email.Contains(searchString)));
            }

       var assignedPatientIds = await assignedPatientsQuery.Select(p => p.Id).ToListAsync();
         var assignedPatients = await assignedPatientsQuery.ToListAsync();

    // Batch query: Get latest frame per patient
   var latestFrames = await _context.PressureFrames
      .Where(f => assignedPatientIds.Contains(f.PatientUserId))
    .GroupBy(f => f.PatientUserId)
      .Select(g => g.OrderByDescending(f => f.Timestamp).FirstOrDefault())
                .ToListAsync();

            // Batch query: Check for alerts in last 24 hours per patient
          var cutoffTime = DateTime.UtcNow.AddHours(-24);
          var patientsWithAlerts = await _context.PressureFrames
                .Where(f => assignedPatientIds.Contains(f.PatientUserId)
         && f.IsAlertFlagged
   && f.Timestamp >= cutoffTime)
                .Select(f => f.PatientUserId)
         .Distinct()
      .ToListAsync();

            // Batch query: Count messages per patient
  var messageCounts = await _context.Comments
        .Where(c => assignedPatientIds.Contains(c.PatientUserId))
         .GroupBy(c => c.PatientUserId)
  .Select(g => new { PatientId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.PatientId, x => x.Count);

          // Build view model with pre-fetched data
        var viewModel = new List<PatientListItemViewModel>();

    foreach (var patient in assignedPatients)
  {
       var lastFrame = latestFrames.FirstOrDefault(f => f?.PatientUserId == patient.Id);
   bool hasAlert = patientsWithAlerts.Contains(patient.Id);
 int msgCount = messageCounts.GetValueOrDefault(patient.Id, 0);

      // Calculate risk score (0-10) based on peak pressure
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
          LastUpdate = lastFrame?.Timestamp ?? DateTime.UtcNow, // Use current time if no data
            RiskScore = riskScore,
          MessageCount = msgCount
           });
  }

    ViewBag.SearchString = searchString;
          return View(viewModel);
        }

        // ========================================================================
        // PATIENT DETAIL VIEW
        // ========================================================================

        // Displays detailed information for a specific patient.
      // Shows pressure data, heatmap, trend chart, and communication history.
   // Only allows viewing patients assigned to this clinician.
        // param: id - The patient's user ID
public async Task<IActionResult> PatientDetail(string id)
        {
     if (string.IsNullOrEmpty(id)) return NotFound();

            // Security check: verify the clinician is assigned to this patient
      var clinician = await _userManager.GetUserAsync(User);
     if (clinician == null) return NotFound();

     bool isAssigned = await _context.ClinicianPatientMaps
          .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == id);

    if (!isAssigned) return Forbid();

            // Get patient details
            var patient = await _context.Users.FindAsync(id);
          if (patient == null) return NotFound();

            // Get or create patient profile with default thresholds
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

            // Get latest pressure frame for heatmap
            var latestFrame = await _context.PressureFrames
           .Where(f => f.PatientUserId == id)
         .OrderByDescending(f => f.Timestamp)
                .FirstOrDefaultAsync();

        // Get history for trend chart (latest 100 frames)
        var history = await _context.PressureFrames
   .Where(f => f.PatientUserId == id)
       .OrderByDescending(f => f.Timestamp)
              .Take(100)
   .OrderBy(f => f.Timestamp)
     .ToListAsync();

          // Get recent comments with replies for communication panel
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

        // ========================================================================
        // PATIENT SETTINGS
  // ========================================================================

        // Updates the alert thresholds for a patient.
        // Allows clinicians to customize pressure and area thresholds.
        // param: profileId - The patient profile ID to update
        // param: highPressureThreshold - New high pressure alert threshold
        // param: minAlertArea - New minimum blob size for alerts
      [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(int profileId, int highPressureThreshold, int minAlertArea)
        {
    var profile = await _context.PatientProfiles.FindAsync(profileId);
 if (profile == null) return NotFound();

        // Security check: verify clinician has access to this patient
   var clinician = await _userManager.GetUserAsync(User);
            if (clinician == null) return NotFound();

            bool isAssigned = await _context.ClinicianPatientMaps
        .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == profile.PatientUserId);

      if (!isAssigned) return Forbid();

    // Validate threshold values
        highPressureThreshold = Math.Clamp(highPressureThreshold, 1, 255);
 minAlertArea = Math.Clamp(minAlertArea, 1, 1024);

            // Update the threshold settings
            profile.HighPressureThreshold = highPressureThreshold;
     profile.MinAlertArea = minAlertArea;

     _context.Update(profile);
            await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "Settings updated successfully.";
    return RedirectToAction("PatientDetail", new { id = profile.PatientUserId });
        }

        // ========================================================================
        // COMMUNICATION
        // ========================================================================

        // Posts a comment or reply on a patient's record.
        // Enables clinician-patient communication.
        // param: patientId - The patient this comment is about
        // param: commentText - The text content of the comment
        // param: parentId - Optional parent comment ID for replies
      [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToComment(string patientId, string commentText, int? parentId)
        {
            // Validate required fields
     if (string.IsNullOrWhiteSpace(commentText) || string.IsNullOrEmpty(patientId))
      {
       TempData["ErrorMessage"] = "Comment text and patient ID are required.";
  return RedirectToAction("PatientDetail", new { id = patientId });
 }

          var clinician = await _userManager.GetUserAsync(User);
          if (clinician == null) return NotFound();

     // Security check: verify clinician has access to this patient
            bool isAssigned = await _context.ClinicianPatientMaps
                .AnyAsync(m => m.ClinicianUserId == clinician.Id && m.PatientUserId == patientId);

       if (!isAssigned) return Forbid();

            // Validate parent comment if provided
 if (parentId.HasValue)
{
                var parentExists = await _context.Comments
 .AnyAsync(c => c.CommentId == parentId.Value && c.PatientUserId == patientId);
     
           if (!parentExists)
{
          parentId = null; // Parent doesn't exist, create as top-level
    }
            }

      // Create the comment
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
