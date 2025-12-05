using Sensore.Models;
using System.Text.Json;

namespace Sensore.Services
{
    public class CsvIngestionService
    {
        private readonly PressureAnalysisService _analysisService;

        public CsvIngestionService(PressureAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }

        public List<PressureFrame> ParseCsv(string filePath, string patientUserId, DateTime startTimestamp)
        {
            var frames = new List<PressureFrame>();

            if (!File.Exists(filePath)) return frames;

            var lines = File.ReadAllLines(filePath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            // The CSV format: 32 rows = 1 frame.
            int rowsPerFrame = 32;
            int totalFrames = lines.Count / rowsPerFrame;

            for (int i = 0; i < totalFrames; i++)
            {
                // 1. Build the Matrix
                int[][] matrix = new int[32][];

                for (int row = 0; row < 32; row++)
                {
                    // Calculate actual line index in the CSV
                    int lineIndex = (i * rowsPerFrame) + row;
                    string line = lines[lineIndex];

                    // Parse comma-separated values
                    int[] rowValues = line.Split(',')
                                          .Select(val => int.TryParse(val, out int v) ? v : 0)
                                          .ToArray();

                    // Safety check: ensure we have 32 columns
                    if (rowValues.Length < 32)
                    {
                        Array.Resize(ref rowValues, 32);
                    }

                    matrix[row] = rowValues;
                }

                // 2. Analyze the Matrix (The Math)
                // Using default thresholds: highThreshold=150, minBlobSize=10, contactThreshold=3
                var analysis = _analysisService.AnalyzeFrame(
                    matrix,
                    highThreshold: 150,
                    minBlobSize: 10,
                    contactThreshold: 3
                );

                // 3. Create the Frame Object
                var frame = new PressureFrame
                {
                    PatientUserId = patientUserId,
                    Timestamp = startTimestamp.AddSeconds(i), // 1 frame = 1 second
                    PressureDataJson = JsonSerializer.Serialize(matrix),
                    PeakPressureIndex = analysis.PeakPressure,
                    ContactAreaPercent = analysis.ContactAreaPercent,
                    IsAlertFlagged = analysis.IsAlertFlagged,
                    ZonalContactAreaJson = "{}" // Placeholder for nice-to-have
                };

                frames.Add(frame);
            }

            return frames;
        }
    }
}