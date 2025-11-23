using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    /// <summary>
    /// Controller for managing clinician-patient relationships.
    /// </summary>
    [Authorize]
    public class CliniciansController : Controller
    {
   private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CliniciansController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
  {
        _context = context;
     _userManager = userManager;
        }

        // GET: Clinicians
        [Authorize(Roles = "Admin,Clinician")]
      public async Task<IActionResult> Index()
   {
       var clinicians = await _userManager.GetUsersInRoleAsync("Clinician");
 return View(clinicians);
   }

        // GET: Clinicians/Details/5
        [Authorize(Roles = "Admin,Clinician")]
      public async Task<IActionResult> Details(string id)
    {
            if (id == null)
    {
     return NotFound();
        }

   var clinician = await _context.Users
   .Include(u => u.PatientsAssigned)
      .ThenInclude(p => p.PatientUser)
     .ThenInclude(p => p.PatientProfile)
  .FirstOrDefaultAsync(u => u.Id == id);

         if (clinician == null)
 {
         return NotFound();
     }

     return View(clinician);
 }

  // GET: Clinicians/MyPatients
        [Authorize(Roles = "Clinician")]
      public async Task<IActionResult> MyPatients()
        {
       var currentUser = await _userManager.GetUserAsync(User);
   
    var patients = await _context.ClinicianPatientMaps
  .Include(c => c.PatientUser)
  .ThenInclude(p => p.PatientProfile)
    .Where(c => c.ClinicianUserId == currentUser.Id)
.Select(c => c.PatientUser)
       .ToListAsync();

  return View(patients);
        }

     // GET: Clinicians/AssignPatient
   [Authorize(Roles = "Admin,Clinician")]
      public async Task<IActionResult> AssignPatient(string clinicianId)
     {
     if (string.IsNullOrEmpty(clinicianId))
            {
  var currentUser = await _userManager.GetUserAsync(User);
 clinicianId = currentUser.Id;
  }

            var clinician = await _context.Users.FindAsync(clinicianId);
     if (clinician == null)
            {
    return NotFound();
    }

// Get all patients not already assigned to this clinician
      var assignedPatientIds = await _context.ClinicianPatientMaps
     .Where(c => c.ClinicianUserId == clinicianId)
           .Select(c => c.PatientUserId)
       .ToListAsync();

   var availablePatients = await _context.Users
   .Include(u => u.PatientProfile)
     .Where(u => u.PatientProfile != null && !assignedPatientIds.Contains(u.Id))
     .ToListAsync();

     ViewBag.Clinician = clinician;
      ViewBag.AvailablePatients = availablePatients;

     return View();
}

        // POST: Clinicians/AssignPatient
   [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Clinician")]
     public async Task<IActionResult> AssignPatient(string clinicianId, string patientId)
  {
  if (string.IsNullOrEmpty(clinicianId) || string.IsNullOrEmpty(patientId))
 {
      return BadRequest("Clinician ID and Patient ID are required");
 }

      // Check if assignment already exists
    var existingMapping = await _context.ClinicianPatientMaps
        .FirstOrDefaultAsync(c => c.ClinicianUserId == clinicianId && c.PatientUserId == patientId);

   if (existingMapping != null)
      {
   TempData["Message"] = "This patient is already assigned to the clinician.";
    return RedirectToAction(nameof(Details), new { id = clinicianId });
 }

     var mapping = new ClinicianPatientMap
     {
       ClinicianUserId = clinicianId,
    PatientUserId = patientId
   };

    _context.ClinicianPatientMaps.Add(mapping);
await _context.SaveChangesAsync();

     TempData["Message"] = "Patient assigned successfully.";
            return RedirectToAction(nameof(Details), new { id = clinicianId });
        }

        // POST: Clinicians/UnassignPatient
        [HttpPost]
 [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Clinician")]
public async Task<IActionResult> UnassignPatient(string clinicianId, string patientId)
     {
         if (string.IsNullOrEmpty(clinicianId) || string.IsNullOrEmpty(patientId))
         {
      return BadRequest("Clinician ID and Patient ID are required");
      }

       var mapping = await _context.ClinicianPatientMaps
   .FirstOrDefaultAsync(c => c.ClinicianUserId == clinicianId && c.PatientUserId == patientId);

      if (mapping == null)
            {
  return NotFound();
     }

     _context.ClinicianPatientMaps.Remove(mapping);
     await _context.SaveChangesAsync();

    TempData["Message"] = "Patient unassigned successfully.";
    return RedirectToAction(nameof(Details), new { id = clinicianId });
   }

        // GET: Clinicians/PatientList/clinicianId
        [Authorize(Roles = "Admin,Clinician")]
 public async Task<IActionResult> PatientList(string id)
        {
if (string.IsNullOrEmpty(id))
     {
        // If no ID provided, use current user
  var currentUser = await _userManager.GetUserAsync(User);
     id = currentUser.Id;
     }

var clinician = await _context.Users.FindAsync(id);
    if (clinician == null)
  {
    return NotFound();
 }

   var patients = await _context.ClinicianPatientMaps
    .Include(c => c.PatientUser)
                .ThenInclude(p => p.PatientProfile)
     .Where(c => c.ClinicianUserId == id)
    .Select(c => c.PatientUser)
 .ToListAsync();

   ViewBag.Clinician = clinician;

       return View(patients);
        }
    }
}
