using Microsoft.AspNetCore.Identity;
using Sensore.Models;
using System.Text.Json;

namespace Sensore.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // 1. Ensure Roles Exist
            string[] roleNames = { "Admin", "Clinician", "Patient" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                }
            }

            // 2. Create Admin User
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

            // 3. Create Clinician
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

            // 4. Create Patient
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

                    // 5. Create Patient Profile
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