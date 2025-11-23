using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;
using System.Text.Json;

namespace Sensore.Controllers
{
    /// <summary>
    /// Controller for managing pressure sensor data frames.
    /// </summary>
    [Authorize]
    public class PressureFramesController : Controller
  {
        private readonly ApplicationDbContext _context;

        public PressureFramesController(ApplicationDbContext context)
        {
 _context = context;
        }

        // GET: PressureFrames
     public async Task<IActionResult> Index(string patientId, DateTime? startDate, DateTime? endDate)
        {
var query = _context.PressureFrames
   .Include(p => p.PatientUser)
      .AsQueryable();

    if (!string.IsNullOrEmpty(patientId))
            {
       query = query.Where(p => p.PatientUserId == patientId);
          }

        if (startDate.HasValue)
            {
     query = query.Where(p => p.Timestamp >= startDate.Value);
         }

            if (endDate.HasValue)
            {
 query = query.Where(p => p.Timestamp <= endDate.Value);
            }

            var frames = await query
       .OrderByDescending(p => p.Timestamp)
     .Take(100)
       .ToListAsync();

      ViewBag.PatientId = patientId;
     ViewBag.StartDate = startDate;
    ViewBag.EndDate = endDate;

    return View(frames);
  }

        // GET: PressureFrames/Details/5
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

        // GET: PressureFrames/PatientData/patientId
        public async Task<IActionResult> PatientData(string id, int days = 7)
        {
          if (string.IsNullOrEmpty(id))
   {
                return NotFound();
            }

            var patient = await _context.Users
.Include(u => u.PatientProfile)
         .FirstOrDefaultAsync(u => u.Id == id);

        if (patient == null)
     {
                return NotFound();
            }

            var startDate = DateTime.UtcNow.AddDays(-days);
   var frames = await _context.PressureFrames
    .Where(p => p.PatientUserId == id && p.Timestamp >= startDate)
    .OrderByDescending(p => p.Timestamp)
   .ToListAsync();

            ViewBag.Patient = patient;
     ViewBag.Days = days;

         return View(frames);
        }

        // GET: PressureFrames/Alerts
        public async Task<IActionResult> Alerts(string patientId)
        {
    var query = _context.PressureFrames
     .Include(p => p.PatientUser)
             .Where(p => p.IsAlertFlagged);

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

        // GET: API endpoint for chart data
        [HttpGet]
        public async Task<IActionResult> GetChartData(string patientId, DateTime? startDate, DateTime? endDate)
   {
            if (string.IsNullOrEmpty(patientId))
    {
return BadRequest("Patient ID is required");
   }

       var query = _context.PressureFrames
 .Where(p => p.PatientUserId == patientId);

         if (startDate.HasValue)
        {
      query = query.Where(p => p.Timestamp >= startDate.Value);
      }

          if (endDate.HasValue)
      {
   query = query.Where(p => p.Timestamp <= endDate.Value);
            }

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

        // POST: PressureFrames/Create
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

        // POST: PressureFrames/Delete/5
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
