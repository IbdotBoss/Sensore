using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    /// <summary>
  /// Controller for managing patient data and profiles.
    /// </summary>
    [Authorize]
    public class PatientsController : Controller
    {
 private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

        public PatientsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
            _context = context;
            _userManager = userManager;
        }

        // GET: Patients
        public async Task<IActionResult> Index()
    {
            var patients = await _context.Users
    .Include(u => u.PatientProfile)
          .Where(u => u.PatientProfile != null)
    .ToListAsync();

            return View(patients);
        }

 // GET: Patients/Details/5
   public async Task<IActionResult> Details(string id)
    {
     if (id == null)
       {
   return NotFound();
         }

   var patient = await _context.Users
   .Include(u => u.PatientProfile)
  .Include(u => u.CliniciansAssigned)
       .ThenInclude(c => c.ClinicianUser)
          .FirstOrDefaultAsync(u => u.Id == id);

    if (patient == null || patient.PatientProfile == null)
         {
    return NotFound();
      }

return View(patient);
        }

        // GET: Patients/Profile/5
   public async Task<IActionResult> Profile(string id)
        {
            if (id == null)
            {
   return NotFound();
            }

   var profile = await _context.PatientProfiles
                .Include(p => p.PatientUser)
    .FirstOrDefaultAsync(p => p.PatientUserId == id);

    if (profile == null)
        {
   return NotFound();
            }

            return View(profile);
        }

        // GET: Patients/EditProfile/5
        public async Task<IActionResult> EditProfile(string id)
        {
            if (id == null)
            {
      return NotFound();
   }

            var profile = await _context.PatientProfiles
   .FirstOrDefaultAsync(p => p.PatientUserId == id);

     if (profile == null)
    {
        return NotFound();
            }

         return View(profile);
        }

   // POST: Patients/EditProfile/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string id, PatientProfile profile)
        {
       if (id != profile.PatientUserId)
            {
     return NotFound();
      }

      if (ModelState.IsValid)
   {
     try
     {
          _context.Update(profile);
        await _context.SaveChangesAsync();
       }
 catch (DbUpdateConcurrencyException)
{
        if (!PatientProfileExists(profile.PatientUserId))
           {
           return NotFound();
               }
      else
                    {
     throw;
          }
 }
                return RedirectToAction(nameof(Details), new { id = profile.PatientUserId });
}
       return View(profile);
 }

 private bool PatientProfileExists(string id)
        {
        return _context.PatientProfiles.Any(e => e.PatientUserId == id);
        }
  }
}
