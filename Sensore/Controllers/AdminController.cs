using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    // Handles all administrative functions for the Sensore system.
  // Admins can create and manage user accounts, assign roles,
    // and link patients to their clinicians.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
 private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
     _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Shows the user management dashboard with all system users.
        // Displays each user's name, email, and role in a searchable table.
    public async Task<IActionResult> Index()
        {
       var users = await _userManager.Users.ToListAsync();
        var model = new List<UserListViewModel>();

      foreach (var user in users)
{
        var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserListViewModel
           {
               Id = user.Id,
  Email = user.Email,
                    FullName = user.FullName ?? "No Name",
        Role = roles.FirstOrDefault() ?? "None"
        });
   }

    return View(model);
        }

        // Shows the form to create a new user account.
        // Admins can set the user's name, email, password, and role.
        public async Task<IActionResult> Create()
        {
      var roles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
            
   var model = new CreateUserViewModel
          {
                RoleList = new SelectList(roles)
            };

            return View(model);
        }

        // Creates a new user account in the system.
   // If the role is Patient, a clinical profile with default
        // alert thresholds is automatically created.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
      {
            if (ModelState.IsValid)
       {
      var user = new ApplicationUser
    {
       UserName = model.Email,
     Email = model.Email,
           FullName = model.FullName,
           RoleType = model.SelectedRole,
           EmailConfirmed = true
   };

   var result = await _userManager.CreateAsync(user, model.Password);

           if (result.Succeeded)
        {
 await _userManager.AddToRoleAsync(user, model.SelectedRole);

       // Patients need a clinical profile for alert settings
     if (model.SelectedRole == "Patient")
         {
    var profile = new PatientProfile
     {
         PatientUserId = user.Id,
    HighPressureThreshold = 150,
              MinAlertArea = 10,
 ContactThreshold = 3
  };

              _context.PatientProfiles.Add(profile);
        await _context.SaveChangesAsync();
     }

                    TempData["SuccessMessage"] = $"User '{user.FullName}' created successfully with role '{model.SelectedRole}'.";
                return RedirectToAction(nameof(Index));
         }

       foreach (var error in result.Errors)
         {
         ModelState.AddModelError(string.Empty, error.Description);
   }
         }

       var roles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
    model.RoleList = new SelectList(roles);
return View(model);
        }

        // Shows the edit form for an existing user.
        // Allows changing name, email, role, and password.
    public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
     }

      var user = await _userManager.FindByIdAsync(id);
            if (user == null)
 {
         return NotFound();
    }

         var roles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
         var userRoles = await _userManager.GetRolesAsync(user);
        var currentRole = userRoles.FirstOrDefault() ?? "None";

         var model = new EditUserViewModel
            {
     Id = user.Id,
         Email = user.Email ?? string.Empty,
     FullName = user.FullName ?? string.Empty,
       CurrentRole = currentRole,
       SelectedRole = currentRole,
       RoleList = new SelectList(roles)
      };

         return View(model);
        }

        // Saves changes to a user's account.
 // Handles role changes by creating a patient profile if needed,
        // and resets password if a new one is provided.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
          if (ModelState.IsValid)
   {
       var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
{
              return NotFound();
        }

         user.Email = model.Email;
       user.UserName = model.Email;
       user.FullName = model.FullName;

          var updateResult = await _userManager.UpdateAsync(user);
      if (!updateResult.Succeeded)
        {
          foreach (var error in updateResult.Errors)
    {
              ModelState.AddModelError(string.Empty, error.Description);
        }
             
        var rolesForError = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
      model.RoleList = new SelectList(rolesForError);
  return View(model);
  }

            // Reset password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
               var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                    
             if (!passwordResult.Succeeded)
              {
        foreach (var error in passwordResult.Errors)
   {
             ModelState.AddModelError(string.Empty, error.Description);
    }
       
       var rolesForError = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
            model.RoleList = new SelectList(rolesForError);
          return View(model);
         }
         }

  // Handle role change
     if (model.CurrentRole != model.SelectedRole)
    {
           if (!string.IsNullOrEmpty(model.CurrentRole) && model.CurrentRole != "None")
    {
  await _userManager.RemoveFromRoleAsync(user, model.CurrentRole);
         }

                  await _userManager.AddToRoleAsync(user, model.SelectedRole);
       user.RoleType = model.SelectedRole;
         await _userManager.UpdateAsync(user);

         // Create patient profile if switching to Patient role
 if (model.SelectedRole == "Patient")
       {
         var existingProfile = await _context.PatientProfiles
       .FirstOrDefaultAsync(p => p.PatientUserId == user.Id);

            if (existingProfile == null)
  {
      var profile = new PatientProfile
          {
    PatientUserId = user.Id,
         HighPressureThreshold = 150,
       MinAlertArea = 10,
   ContactThreshold = 3
 };
                _context.PatientProfiles.Add(profile);
         await _context.SaveChangesAsync();
     }
        }
     }

          TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
     return RedirectToAction(nameof(Index));
 }

        var allRoles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
            model.RoleList = new SelectList(allRoles);
            return View(model);
        }

     // Permanently deletes a user and all their associated data.
        // This includes their profile, pressure readings, comments, and
        // any clinician-patient assignments. Admins cannot delete themselves.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
      if (string.IsNullOrEmpty(id))
    {
          return NotFound();
            }

        var user = await _userManager.FindByIdAsync(id);
    if (user == null)
   {
   TempData["ErrorMessage"] = "User not found.";
     return RedirectToAction(nameof(Index));
            }

            // Prevent admins from deleting their own account
         var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == user.Id)
            {
       TempData["ErrorMessage"] = "You cannot delete your own account.";
      return RedirectToAction(nameof(Index));
   }

   try
            {
             // Remove all related data before deleting the user
                
      var profile = await _context.PatientProfiles
        .FirstOrDefaultAsync(p => p.PatientUserId == id);
         if (profile != null)
          {
     _context.PatientProfiles.Remove(profile);
        }

   var clinicianMaps = await _context.ClinicianPatientMaps
           .Where(m => m.ClinicianUserId == id || m.PatientUserId == id)
          .ToListAsync();
     if (clinicianMaps.Any())
             {
     _context.ClinicianPatientMaps.RemoveRange(clinicianMaps);
          }

    var pressureFrames = await _context.PressureFrames
            .Where(f => f.PatientUserId == id)
        .ToListAsync();
     if (pressureFrames.Any())
    {
            _context.PressureFrames.RemoveRange(pressureFrames);
  }

    var comments = await _context.Comments
           .Where(c => c.AuthorUserId == id || c.PatientUserId == id)
                 .ToListAsync();
     if (comments.Any())
          {
         _context.Comments.RemoveRange(comments);
      }

    await _context.SaveChangesAsync();

           var result = await _userManager.DeleteAsync(user);
      if (result.Succeeded)
      {
TempData["SuccessMessage"] = $"User '{user.FullName}' deleted successfully.";
           }
          else
                {
         TempData["ErrorMessage"] = $"Error deleting user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
     }
          }
      catch (Exception ex)
{
        TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
      }

            return RedirectToAction(nameof(Index));
        }

        // Shows the patient-clinician assignment interface.
        // Admins select a clinician, then can add or remove patients
        // from their care list using the available/assigned panels.
        public async Task<IActionResult> PatientLinking(string? clinicianId)
        {
      var clinicians = await _userManager.GetUsersInRoleAsync("Clinician");

         var model = new PatientLinkingViewModel
    {
   SelectedClinicianId = clinicianId,
   ClinicianList = new SelectList(clinicians, "Id", "FullName", clinicianId)
            };

            if (!string.IsNullOrEmpty(clinicianId))
   {
        var assignedIds = await _context.ClinicianPatientMaps
      .Where(m => m.ClinicianUserId == clinicianId)
 .Select(m => m.PatientUserId)
        .ToListAsync();

                model.AssignedPatients = await _context.Users
          .Where(u => assignedIds.Contains(u.Id))
        .ToListAsync();

     var allPatients = await _userManager.GetUsersInRoleAsync("Patient");
     model.AvailablePatients = allPatients
  .Where(p => !assignedIds.Contains(p.Id))
            .ToList();
            }

     return View(model);
        }

     // Assigns or unassigns a patient to/from a clinician.
        // Called via AJAX from the patient linking page when
        // the admin clicks the add or remove button.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLink(string clinicianId, string patientId, string actionType)
     {
            if (string.IsNullOrEmpty(clinicianId) || string.IsNullOrEmpty(patientId)) 
     return BadRequest();

 if (actionType == "Add")
   {
    var exists = await _context.ClinicianPatientMaps
        .AnyAsync(m => m.ClinicianUserId == clinicianId && m.PatientUserId == patientId);

      if (!exists)
       {
        _context.ClinicianPatientMaps.Add(new ClinicianPatientMap
           {
      ClinicianUserId = clinicianId,
    PatientUserId = patientId
       });
      }
   }
            else if (actionType == "Remove")
            {
     var link = await _context.ClinicianPatientMaps
    .FirstOrDefaultAsync(m => m.ClinicianUserId == clinicianId && m.PatientUserId == patientId);

 if (link != null)
           {
         _context.ClinicianPatientMaps.Remove(link);
    }
            }

      await _context.SaveChangesAsync();
            return RedirectToAction("PatientLinking", new { clinicianId = clinicianId });
    }
    }
}