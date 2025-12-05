using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
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

        // 1. List All Users
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

        // 2. Create User (GET)
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).Where(r => r != null).ToListAsync();
            
            var model = new CreateUserViewModel
            {
                RoleList = new SelectList(roles)
            };

            return View(model);
        }

        // 3. Create User (POST)
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

        // 4. Edit User (GET)
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

        // 5. Edit User (POST)
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

                // Update password if provided
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
                    if (!string.IsNullOrEmpty(model.CurrentRole) && model.CurrentRole != "None")
                    {
                        await _userManager.RemoveFromRoleAsync(user, model.CurrentRole);
                    }

                    await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    user.RoleType = model.SelectedRole;
                    await _userManager.UpdateAsync(user);

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

        // 6. Patient Linking UI
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

        // 7. Toggle Link (Add/Remove)
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