using Sensore.Models;
using System.Text.Json;

namespace Sensore.Services
{
    // Service for importing pressure sensor data from CSV files.
    // Parses raw CSV data into PressureFrame objects with calculated metrics.
    // Used during database seeding and data import operations.
    public class CsvIngestionService
    {
        private readonly PressureAnalysisService _analysisService;

        public CsvIngestionService(PressureAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }

        // Parses a CSV file containing pressure sensor data into PressureFrame objects.
        // Each frame consists of 32 rows of 32 comma-separated values.
        // Metrics are calculated during parsing for efficient storage.
        // param: filePath - Path to the CSV file
        // param: patientUserId - The patient this data belongs to
        // param: startTimestamp - Base timestamp for the first frame (subsequent frames are +1 second each)
        // returns: List of PressureFrame objects ready for database insertion
        public List<PressureFrame> ParseCsv(string filePath, string patientUserId, DateTime startTimestamp)
        {
            var frames = new List<PressureFrame>();

            // Verify file exists before processing
            if (!File.Exists(filePath)) return frames;

            // Read all non-empty lines from the CSV
            var lines = File.ReadAllLines(filePath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            // Each frame is 32 rows of data
            int rowsPerFrame = 32;
            int totalFrames = lines.Count / rowsPerFrame;

            // Process each frame
            for (int i = 0; i < totalFrames; i++)
            {
                // ----------------------------------------------------------------
                // STEP 1: Build the 32x32 pressure matrix for this frame
                // ----------------------------------------------------------------
                int[][] matrix = new int[32][];

                for (int row = 0; row < 32; row++)
                {
                    // Calculate the line index in the CSV for this row
                    int lineIndex = (i * rowsPerFrame) + row;
                    string line = lines[lineIndex];

                    // Parse comma-separated values into integers
                    int[] rowValues = line.Split(',')
                                          .Select(val => int.TryParse(val, out int v) ? v : 0)
                                          .ToArray();

                    // Ensure we have exactly 32 columns (pad if needed)
                    if (rowValues.Length < 32)
                    {
                        Array.Resize(ref rowValues, 32);
                    }

                    matrix[row] = rowValues;
                }

                // ----------------------------------------------------------------
                // STEP 2: Analyze the matrix to calculate metrics
                // Uses default thresholds (can be customized per patient later)
                // ----------------------------------------------------------------
                var analysis = _analysisService.AnalyzeFrame(
                    matrix,
                    highThreshold: 150,    // Pressure level that triggers alerts
                    minBlobSize: 10,       // Minimum pixels for a valid pressure blob
                    contactThreshold: 3    // Minimum pressure to count as contact
                );

                // ----------------------------------------------------------------
                // STEP 3: Create the PressureFrame object
                // ----------------------------------------------------------------
                var frame = new PressureFrame
                {
                    PatientUserId = patientUserId,
                    Timestamp = startTimestamp.AddSeconds(i), // Each frame = 1 second
                    PressureDataJson = JsonSerializer.Serialize(matrix),
                    PeakPressureIndex = analysis.PeakPressure,
                    ContactAreaPercent = analysis.ContactAreaPercent,
                    IsAlertFlagged = analysis.IsAlertFlagged,
                    ZonalContactAreaJson = "{}" // Placeholder for future zonal analysis
                };

                frames.Add(frame);
            }

            return frames;
        }
    }
}