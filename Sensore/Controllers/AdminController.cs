using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    // Controller for admin-only functionality.
    // Manages users, roles, and clinician-patient assignments.
    // Only accessible by users with the Admin role.
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

        // ========================================================================
        // USER MANAGEMENT
        // ========================================================================

        // Displays the list of all users in the system.
        // Shows each user's name, email, and assigned role.
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new List<UserListViewModel>();

            // Build view model with role information for each user
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

        // Displays the form for creating a new user.
        public async Task<IActionResult> Create()
        {
            // Get available roles for the dropdown
            var roles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
            
            var model = new CreateUserViewModel
            {
                RoleList = new SelectList(roles)
            };

            return View(model);
        }

        // Creates a new user account with the specified role.
    // Automatically creates a PatientProfile if the role is Patient.
      [HttpPost]
  [ValidateAntiForgeryToken]
   public async Task<IActionResult> Create(CreateUserViewModel model)
     {
            if (ModelState.IsValid)
  {
       // Create the new user
  var user = new ApplicationUser
 {
     UserName = model.Email,
  Email = model.Email,
       FullName = model.FullName,
   RoleType = model.SelectedRole,
        EmailConfirmed = true // Skip email confirmation for admin-created users
      };

       var result = await _userManager.CreateAsync(user, model.Password);

   if (result.Succeeded)
        {
         // Assign the selected role
       await _userManager.AddToRoleAsync(user, model.SelectedRole);

            // Create a patient profile if this is a patient user
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

     // Add any creation errors to ModelState
   foreach (var error in result.Errors)
        {
   ModelState.AddModelError(string.Empty, error.Description);
     }
   }

// Reload roles for the dropdown if validation fails
            var roles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
          model.RoleList = new SelectList(roles);
return View(model);
        }

        // Displays the form for editing an existing user.
        // param: id - The user ID to edit
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

            // Get roles for dropdown and user's current role
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

     // Updates an existing user's details, role, and optionally password.
        // Creates a PatientProfile if role is changed to Patient.
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

        // Update basic user information
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

      // Update password if a new one was provided
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

    // Update role if changed
    if (model.CurrentRole != model.SelectedRole)
     {
       // Remove from old role
      if (!string.IsNullOrEmpty(model.CurrentRole) && model.CurrentRole != "None")
      {
         await _userManager.RemoveFromRoleAsync(user, model.CurrentRole);
      }

     // Add to new role
        await _userManager.AddToRoleAsync(user, model.SelectedRole);
        user.RoleType = model.SelectedRole;
     await _userManager.UpdateAsync(user);

       // Create patient profile if newly assigned to Patient role
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

  // Deletes a user and all related data.
        // Removes profile, mappings, pressure frames, and comments.
   // Admins cannot delete their own account.
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

        // Safety check: prevent self-deletion
  var currentUser = await _userManager.GetUserAsync(User);
   if (currentUser?.Id == user.Id)
   {
      TempData["ErrorMessage"] = "You cannot delete your own account.";
   return RedirectToAction(nameof(Index));
  }

      try
 {
 // Delete related data in order to avoid foreign key violations
                
     // 1. Delete patient profile if exists
    var profile = await _context.PatientProfiles
      .FirstOrDefaultAsync(p => p.PatientUserId == id);
if (profile != null)
          {
       _context.PatientProfiles.Remove(profile);
     }

         // 2. Delete clinician-patient mappings (both directions)
    var clinicianMaps = await _context.ClinicianPatientMaps
     .Where(m => m.ClinicianUserId == id || m.PatientUserId == id)
     .ToListAsync();
    if (clinicianMaps.Any())
      {
 _context.ClinicianPatientMaps.RemoveRange(clinicianMaps);
  }

      // 3. Delete pressure frames for patient users
        var pressureFrames = await _context.PressureFrames
       .Where(f => f.PatientUserId == id)
 .ToListAsync();
        if (pressureFrames.Any())
 {
          _context.PressureFrames.RemoveRange(pressureFrames);
 }

             // 4. Delete all comments (authored and received)
           var comments = await _context.Comments
        .Where(c => c.AuthorUserId == id || c.PatientUserId == id)
     .ToListAsync();
   if (comments.Any())
       {
      _context.Comments.RemoveRange(comments);
       }

  await _context.SaveChangesAsync();

  // Finally, delete the user account
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

  // ========================================================================
        // PATIENT-CLINICIAN ASSIGNMENT
        // ========================================================================

     // Displays the patient linking interface.
        // Allows admins to assign patients to clinicians.
        // param: clinicianId - Optional clinician ID to pre-select
     public async Task<IActionResult> PatientLinking(string? clinicianId)
        {
  // Get all clinicians for the dropdown
        var clinicians = await _userManager.GetUsersInRoleAsync("Clinician");

            var model = new PatientLinkingViewModel
{
          SelectedClinicianId = clinicianId,
     ClinicianList = new SelectList(clinicians, "Id", "FullName", clinicianId)
    };

   // If a clinician is selected, load their assigned and available patients
            if (!string.IsNullOrEmpty(clinicianId))
            {
                // Get IDs of currently assigned patients
        var assignedIds = await _context.ClinicianPatientMaps
    .Where(m => m.ClinicianUserId == clinicianId)
        .Select(m => m.PatientUserId)
  .ToListAsync();

     // Get assigned patient details
    model.AssignedPatients = await _context.Users
       .Where(u => assignedIds.Contains(u.Id))
    .ToListAsync();

     // Get unassigned patients (available for assignment)
           var allPatients = await _userManager.GetUsersInRoleAsync("Patient");
        model.AvailablePatients = allPatients
      .Where(p => !assignedIds.Contains(p.Id))
       .ToList();
    }

       return View(model);
   }

        // Adds or removes a patient assignment for a clinician.
   // param: clinicianId - The clinician to modify
  // param: patientId - The patient to assign/unassign
        // param: actionType - "Add" or "Remove"
        [HttpPost]
      [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLink(string clinicianId, string patientId, string actionType)
        {
 if (string.IsNullOrEmpty(clinicianId) || string.IsNullOrEmpty(patientId)) 
         return BadRequest();

  if (actionType == "Add")
     {
           // Check if assignment already exists to prevent duplicates
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
        // Find and remove the existing assignment
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