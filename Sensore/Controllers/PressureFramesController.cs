using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;
using System.Text.Json;

namespace Sensore.Controllers
{
    // Controller for managing and viewing pressure sensor data frames.
    // Provides listing, filtering, alerts view, and API endpoints for charts.
    // Requires authentication for all actions.
    [Authorize]
    public class PressureFramesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PressureFramesController(ApplicationDbContext context)
        {
         _context = context;
     }

        // ========================================================================
    // LIST AND FILTER FRAMES
        // ========================================================================

        // Lists pressure frames with optional filtering.
 // Supports filtering by patient and date range.
     // Returns the 100 most recent matching frames.
        // param: patientId - Filter by patient (optional)
        // param: startDate - Start of date range (optional)
  // param: endDate - End of date range (optional)
      public async Task<IActionResult> Index(string patientId, DateTime? startDate, DateTime? endDate)
   {
    var query = _context.PressureFrames
           .Include(p => p.PatientUser)
     .AsQueryable();

            // Apply patient filter if specified
     if (!string.IsNullOrEmpty(patientId))
  {
     query = query.Where(p => p.PatientUserId == patientId);
  }

      // Apply date range filters
            if (startDate.HasValue)
    {
       query = query.Where(p => p.Timestamp >= startDate.Value);
    }

 if (endDate.HasValue)
            {
         query = query.Where(p => p.Timestamp <= endDate.Value);
     }

       // Get most recent 100 frames
       var frames = await query
.OrderByDescending(p => p.Timestamp)
                .Take(100)
   .ToListAsync();

 // Pass filter values to view for form persistence
            ViewBag.PatientId = patientId;
      ViewBag.StartDate = startDate;
     ViewBag.EndDate = endDate;

            return View(frames);
  }

     // Displays detailed information for a single pressure frame.
        // Shows all metrics and the raw pressure data.
        // param: id - The frame ID
        public async Task<IActionResult> Details(long? id)
        {
        if (id == null)
        {
       return NotFound();
     }

          var pressureFrame = await _context.PressureFrames
          .Include(p => p.PatientUser)
        .FirstOrDefaultAsync(m => m.FrameId == id);

       if (pressureFrame == null)
       {
     return NotFound();
            }

    return View(pressureFrame);
        }

      // Displays pressure data history for a specific patient.
        // Shows frames from the specified number of days.
        // param: id - The patient's user ID
        // param: days - Number of days of history to show (default: 7)
        public async Task<IActionResult> PatientData(string id, int days = 7)
        {
        if (string.IsNullOrEmpty(id))
            {
          return NotFound();
            }

        // Get patient with profile
            var patient = await _context.Users
          .Include(u => u.PatientProfile)
 .FirstOrDefaultAsync(u => u.Id == id);

          if (patient == null)
          {
        return NotFound();
     }

            // Get frames from the specified time period
       var startDate = DateTime.UtcNow.AddDays(-days);
  var frames = await _context.PressureFrames
    .Where(p => p.PatientUserId == id && p.Timestamp >= startDate)
     .OrderByDescending(p => p.Timestamp)
        .ToListAsync();

         ViewBag.Patient = patient;
        ViewBag.Days = days;

            return View(frames);
 }

        // ========================================================================
        // ALERTS VIEW
     // ========================================================================

        // Displays frames that have triggered alerts.
        // Shows the 50 most recent alert frames.
        // Can be filtered by patient.
        // param: patientId - Filter by patient (optional)
public async Task<IActionResult> Alerts(string patientId)
        {
         var query = _context.PressureFrames
      .Include(p => p.PatientUser)
      .Where(p => p.IsAlertFlagged);

     // Apply patient filter if specified
            if (!string.IsNullOrEmpty(patientId))
          {
       query = query.Where(p => p.PatientUserId == patientId);
     }

         var alerts = await query
          .OrderByDescending(p => p.Timestamp)
             .Take(50)
           .ToListAsync();

  ViewBag.PatientId = patientId;

            return View(alerts);
  }

        // ========================================================================
        // API ENDPOINTS
        // ========================================================================

  // Returns pressure data as JSON for chart visualization.
        // Used by frontend JavaScript to render trend charts.
        // param: patientId - Required: the patient to get data for
   // param: startDate - Start of date range (optional)
        // param: endDate - End of date range (optional)
        // returns: JSON array of data points with timestamp and metrics
      [HttpGet]
        public async Task<IActionResult> GetChartData(string patientId, DateTime? startDate, DateTime? endDate)
  {
      if (string.IsNullOrEmpty(patientId))
            {
         return BadRequest("Patient ID is required");
   }

            var query = _context.PressureFrames
     .Where(p => p.PatientUserId == patientId);

            // Apply date range filters
   if (startDate.HasValue)
         {
      query = query.Where(p => p.Timestamp >= startDate.Value);
    }

            if (endDate.HasValue)
        {
       query = query.Where(p => p.Timestamp <= endDate.Value);
      }

    // Select only the fields needed for charting
            var data = await query
   .OrderBy(p => p.Timestamp)
   .Select(p => new
           {
                    timestamp = p.Timestamp,
     peakPressureIndex = p.PeakPressureIndex,
        contactAreaPercent = p.ContactAreaPercent,
             isAlertFlagged = p.IsAlertFlagged
       })
   .ToListAsync();

            return Json(data);
   }

        // ========================================================================
        // CREATE AND DELETE
        // ========================================================================

     // Creates a new pressure frame record.
   // Typically used by data import processes.
        [HttpPost]
        [ValidateAntiForgeryToken]
   public async Task<IActionResult> Create(PressureFrame pressureFrame)
        {
            if (ModelState.IsValid)
   {
          _context.Add(pressureFrame);
            await _context.SaveChangesAsync();
   return RedirectToAction(nameof(Index));
            }
       return View(pressureFrame);
        }

        // Deletes a pressure frame from the database.
        // param: id - The frame ID to delete
        [HttpPost, ActionName("Delete")]
      [ValidateAntiForgeryToken]
 public async Task<IActionResult> DeleteConfirmed(long id)
        {
        var pressureFrame = await _context.PressureFrames.FindAsync(id);
          if (pressureFrame != null)
 {
          _context.PressureFrames.Remove(pressureFrame);
 await _context.SaveChangesAsync();
            }

       return RedirectToAction(nameof(Index));
    }
 }
}
