using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sensore.Models;
using Sensore.Services;

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

                // Ensure the DB is created
                context.Database.EnsureCreated();

                // Check if we already have frames
                if (context.PressureFrames.Any())
                {
                    return; // DB has been seeded
                }

                // --- Seed Logic ---
                var analysisService = new PressureAnalysisService();
                var csvService = new CsvIngestionService(analysisService);

                // Find the main patient created by Member 1
                var patient = await userManager.FindByEmailAsync("patient@sensore.com");
                if (patient != null)
                {
                    // Define path to CSV (Member 2 needs to put a file here!)
                    // In a real scenario, use IWebHostEnvironment to get the path
                    string seedsDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Seeds");
                    string csvPath = Path.Combine(seedsDirectory, "mock_data.csv");

                    // Ensure directory exists
                    if (!Directory.Exists(seedsDirectory)) Directory.CreateDirectory(seedsDirectory);

                    // Create a realistic dummy CSV if it doesn't exist
                    if (!File.Exists(csvPath))
                    {
                        CreateRealisticDummyCsv(csvPath);
                    }

                    // Run the Ingestion
                    var frames = csvService.ParseCsv(csvPath, patient.Id, DateTime.UtcNow.AddHours(-1));

                    if (frames.Any())
                    {
                        await context.PressureFrames.AddRangeAsync(frames);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a realistic CSV file using Gaussian blobs to simulate human pressure distribution.
        /// Generates data for multiple frames with breathing/movement simulation.
        /// </summary>
        private static void CreateRealisticDummyCsv(string path)
        {
            using (var writer = new StreamWriter(path))
            {
                // Generate 100 frames (simulates ~1.5 minutes of data if 1 frame = 1 second)
                int numFrames = 100;
                
                for (int frameIndex = 0; frameIndex < numFrames; frameIndex++)
                {
                    // Generate realistic human-shaped pressure distribution
                    int[][] matrix = RealisticDataGenerator.GenerateHumanShape(frameIndex);

                    // Write all 32 rows for this frame
                    for (int row = 0; row < 32; row++)
                    {
                        var rowData = string.Join(",", matrix[row]);
                        writer.WriteLine(rowData);
                    }
                }
            }
        }

        /// <summary>
        /// Legacy method: Creates random noise CSV (kept for reference).
        /// Use CreateRealisticDummyCsv() instead for better visualization.
        /// </summary>
        [Obsolete("Use CreateRealisticDummyCsv() for realistic pressure data")]
        private static void CreateDummyCsv(string path)
        {
            // Generates a random 32x32 CSV for testing if no file is provided
            using (var writer = new StreamWriter(path))
            {
                var rnd = new Random();
                for (int i = 0; i < 64; i++) // 2 Frames (32 rows * 2)
                {
                    var row = string.Join(",", Enumerable.Range(0, 32).Select(_ => rnd.Next(0, 256)));
                    writer.WriteLine(row);
                }
            }
        }
    }
}