using Microsoft.AspNetCore.Identity;
using Sensore.Models;
using System.Text.Json;

namespace Sensore.Data
{
    // Initializes the database with essential data on first run.
    // Creates default roles and a system administrator account.
    // Called during application startup.
    public static class DbInitializer
    {
        // Creates roles and the default admin user if they don't exist.
        // param: serviceProvider - DI service provider
        // param: userManager - Identity user manager
        // param: roleManager - Identity role manager
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // ----------------------------------------------------------------
            // STEP 1: Create application roles
            // Three roles: Admin, Clinician, Patient
            // ----------------------------------------------------------------
            string[] roleNames = { "Admin", "Clinician", "Patient" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                }
            }

            // ----------------------------------------------------------------
            // STEP 2: Create default Admin user
            // This ensures there's always an admin who can access the system
            // Default credentials: admin@sensore.com / Admin@123
            // ----------------------------------------------------------------
            if (await userManager.FindByEmailAsync("admin@sensore.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@sensore.com",
                    Email = "admin@sensore.com",
                    FullName = "System Administrator",
                    RoleType = "Admin",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ----------------------------------------------------------------
            // STEP 3: Create default Clinician user for testing
            // Default credentials: dr.smith@sensore.com / Doctor@123
            // ----------------------------------------------------------------
            if (await userManager.FindByEmailAsync("dr.smith@sensore.com") == null)
            {
                var clinician = new ApplicationUser
                {
                    UserName = "dr.smith@sensore.com",
                    Email = "dr.smith@sensore.com",
                    FullName = "Dr. John Smith",
                    RoleType = "Clinician",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(clinician, "Doctor@123");
                await userManager.AddToRoleAsync(clinician, "Clinician");
            }

            // ----------------------------------------------------------------
            // STEP 4: Create default Patient user for testing
            // Default credentials: patient@sensore.com / Patient@123
            // Also creates the patient's clinical profile
            // ----------------------------------------------------------------
            if (await userManager.FindByEmailAsync("patient@sensore.com") == null)
            {
                var patient = new ApplicationUser
                {
                    UserName = "patient@sensore.com",
                    Email = "patient@sensore.com",
                    FullName = "Jane Doe",
                    RoleType = "Patient",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(patient, "Patient@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(patient, "Patient");

                    // Create the patient's clinical profile with default thresholds
                    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                    var profile = new PatientProfile
                    {
                        PatientUserId = patient.Id,
                        HighPressureThreshold = 150,
                        MinAlertArea = 10,
                        ContactThreshold = 3
                    };
                    context.PatientProfiles.Add(profile);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}