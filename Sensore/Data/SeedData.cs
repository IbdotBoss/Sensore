using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sensore.Models;
using Sensore.Services;
using System.Globalization;

namespace Sensore.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var analysisService = scope.ServiceProvider.GetRequiredService<PressureAnalysisService>();
                
                context.Database.EnsureCreated();

                // Ensure roles exist
                await EnsureRolesExist(roleManager);

                // 1. Define the Mapping (Hex ID from Filename -> Target User)
                var patientMap = new Dictionary<string, (string Name, string Email)>
                {
                    { "1c0fd777", ("Bruce Wayne", "bruce.wayne@sensore.com") },
                    { "71e66ab3", ("Khalil Umar", "khalil.umar@sensore.com") },
                    { "543d4676", ("Zarah Haroon", "zarah.haroon@sensore.com") },
                    { "d13043b3", ("Vanessa Denvel", "vanessa.denvel@sensore.com") },
                    { "de0e9b2c", ("Bona Saint", "bona.saint@sensore.com") }
                };

                // 2. Create Users & Profiles
                Console.WriteLine("=== Creating Seed Patients ===");
                foreach (var entry in patientMap)
                {
                    var hexId = entry.Key;
                    var name = entry.Value.Name;
                    var email = entry.Value.Email;

                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = name,
                            RoleType = "Patient",
                            EmailConfirmed = true
                        };
                        
                        var result = await userManager.CreateAsync(user, "Patient@123");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Patient");
                            Console.WriteLine($"✓ Created patient: {name} ({email})");

                            // Create Profile
                            context.PatientProfiles.Add(new PatientProfile
                            {
                                PatientUserId = user.Id,
                                HighPressureThreshold = 150,
                                MinAlertArea = 10,
                                ContactThreshold = 3
                            });
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            Console.WriteLine($"✗ Failed to create {name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"○ Patient already exists: {name} ({email})");
                    }
                }

                // 3. Process CSV Files
                Console.WriteLine("\n=== Processing CSV Seed Files ===");
                
                // Try multiple possible paths for the Seeds folder
                var possiblePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "Data", "Seeds"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seeds"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Seeds")
                };

                string? seedsDirectory = null;
                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        seedsDirectory = path;
                        break;
                    }
                }

                if (seedsDirectory == null)
                {
                    Console.WriteLine("⚠ Warning: Seeds directory not found. Tried paths:");
                    foreach (var path in possiblePaths)
                    {
                        Console.WriteLine($"  - {path}");
                    }
                    Console.WriteLine("Skipping CSV file seeding.");
                    return;
                }

                Console.WriteLine($"📁 Seeds directory: {seedsDirectory}");
                
                var csvFiles = Directory.GetFiles(seedsDirectory, "*.csv");
                
                if (csvFiles.Length == 0)
                {
                    Console.WriteLine("⚠ No CSV files found in Seeds directory.");
                    return;
                }

                Console.WriteLine($"Found {csvFiles.Length} CSV file(s) to process.");

                var csvService = new CsvIngestionService(analysisService);
                int processedFiles = 0;
                int skippedFiles = 0;
                int errorFiles = 0;

                foreach (var filePath in csvFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath); // e.g., "1c0fd777_20251011"
                        var parts = fileName.Split('_');

                        if (parts.Length == 2)
                        {
                            string hexId = parts[0].ToLower();
                            string dateString = parts[1]; // "20251011"

                            // Find the user for this file
                            if (patientMap.ContainsKey(hexId))
                            {
                                var email = patientMap[hexId].Email;
                                var user = await userManager.FindByEmailAsync(email);

                                if (user != null)
                                {
                                    // Parse Date (YYYYMMDD)
                                    if (DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate))
                                    {
                                        // Check if we already seeded this specific date for this user
                                        bool alreadySeeded = await context.PressureFrames
                                            .AnyAsync(f => f.PatientUserId == user.Id && f.Timestamp.Date == fileDate.Date);

                                        if (!alreadySeeded)
                                        {
                                            Console.WriteLine($"  Processing: {Path.GetFileName(filePath)} for {patientMap[hexId].Name}...");
                                            
                                            var frames = csvService.ParseCsv(filePath, user.Id, fileDate);
                                            
                                            if (frames.Any())
                                            {
                                                await context.PressureFrames.AddRangeAsync(frames);
                                                await context.SaveChangesAsync();
                                                Console.WriteLine($"    ✓ Saved {frames.Count} frames");
                                                processedFiles++;
                                            }
                                            else
                                            {
                                                Console.WriteLine($"    ⚠ No valid frames found in file");
                                                skippedFiles++;
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"  ○ Skipping {Path.GetFileName(filePath)} - already seeded for this date");
                                            skippedFiles++;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"  ✗ Invalid date format in filename: {fileName}");
                                        errorFiles++;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"  ✗ User not found for hex ID: {hexId}");
                                    errorFiles++;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"  ⚠ Unknown hex ID in filename: {hexId}");
                                errorFiles++;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  ✗ Invalid filename format: {fileName} (expected: HexId_YYYYMMDD.csv)");
                            errorFiles++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ Error processing {Path.GetFileName(filePath)}: {ex.Message}");
                        errorFiles++;
                    }
                }

                Console.WriteLine($"\n=== Seeding Complete ===");
                Console.WriteLine($"Processed: {processedFiles} | Skipped: {skippedFiles} | Errors: {errorFiles}");
            }
        }

        private static async Task EnsureRolesExist(RoleManager<ApplicationRole> roleManager)
        {
            string[] roleNames = { "Admin", "Clinician", "Patient" };
            
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                }
            }
        }
    }
}