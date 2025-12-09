using System.Text.Json;

namespace Sensore.Services
{
    // Service for analyzing pressure sensor data frames.
    // Performs blob detection and calculates metrics for pressure monitoring.
  // Used during data ingestion to pre-compute values for faster dashboard display.
    public class PressureAnalysisService
 {
    // The sensor produces a 32x32 matrix of pressure values
  private const int MATRIX_SIZE = 32;

        // Contains the results of analyzing a pressure frame.
public struct AnalysisResult
  {
            // The highest pressure value found in valid pressure blobs (0-255).
   public int PeakPressure { get; set; }

    // Percentage of sensor area showing contact (0-100%).
            public double ContactAreaPercent { get; set; }

          // Whether this frame should trigger a high-pressure alert.
    public bool IsAlertFlagged { get; set; }
        }

        // Analyzes a pressure matrix to extract key metrics.
        // Performs contact area calculation and blob detection for alerts.
        // param: matrix - 32x32 array of pressure values (0-255)
  // param: highThreshold - Pressure value above which triggers an alert
        // param: minBlobSize - Minimum connected pixels to consider a valid blob
     // param: contactThreshold - Minimum pressure to consider as contact
 // returns: Analysis results with metrics and alert status
        public AnalysisResult AnalyzeFrame(int[][] matrix, int highThreshold, int minBlobSize, int contactThreshold)
        {
     var result = new AnalysisResult();

    // ----------------------------------------------------------------
            // STEP 1: Calculate Contact Area Percentage
      // Count pixels above contact threshold as "in contact"
     // ----------------------------------------------------------------
            int contactPixels = 0;
  int totalPixels = MATRIX_SIZE * MATRIX_SIZE;

    for (int r = 0; r < MATRIX_SIZE; r++)
{
      for (int c = 0; c < MATRIX_SIZE; c++)
       {
   if (matrix[r][c] > contactThreshold)
          {
      contactPixels++;
     }
        }
            }
    result.ContactAreaPercent = Math.Round(((double)contactPixels / totalPixels) * 100, 2);

       // ----------------------------------------------------------------
       // STEP 2: Blob Detection using Connected Component Labeling
   // Find connected regions of pressure and analyze each blob
 // Only blobs >= minBlobSize are considered valid (reduces noise)
      // ----------------------------------------------------------------
    var visited = new bool[MATRIX_SIZE, MATRIX_SIZE];
  int maxPressureInValidBlobs = 0;
     bool alertTriggered = false;

    for (int r = 0; r < MATRIX_SIZE; r++)
       {
       for (int c = 0; c < MATRIX_SIZE; c++)
        {
      // Start a new blob search if pixel has pressure and hasn't been visited
     if (matrix[r][c] > contactThreshold && !visited[r, c])
        {
  // Use BFS to find all connected pixels in this blob
          var blob = GetBlob(matrix, r, c, visited, contactThreshold);

         // Only consider blobs that meet minimum size requirement
             // This filters out small noise artifacts
    if (blob.PixelCount >= minBlobSize)
        {
                 // Track the maximum pressure across all valid blobs
       if (blob.MaxPressure > maxPressureInValidBlobs)
       {
maxPressureInValidBlobs = blob.MaxPressure;
    }

    // Check if this blob triggers an alert
   if (blob.MaxPressure > highThreshold)
 {
        alertTriggered = true;
    }
    }
        }
            }
    }

            result.PeakPressure = maxPressureInValidBlobs;
        result.IsAlertFlagged = alertTriggered;

 return result;
        }

 // Finds all connected pixels in a pressure blob using BFS.
      // Uses 4-connectivity (up, down, left, right neighbors).
        // param: matrix - The pressure data matrix
        // param: startR - Starting row position
     // param: startC - Starting column position
    // param: visited - Tracking array for visited pixels
        // param: threshold - Minimum pressure to include in blob
      // returns: Tuple with pixel count and maximum pressure in the blob
        private (int PixelCount, int MaxPressure) GetBlob(int[][] matrix, int startR, int startC, bool[,] visited, int threshold)
     {
   // BFS queue starting from the initial pixel
     var q = new Queue<(int r, int c)>();
    q.Enqueue((startR, startC));
   visited[startR, startC] = true;

   int count = 0;
            int maxP = 0;

      // Direction vectors for 4-connectivity (up, down, left, right)
int[] dr = { -1, 1, 0, 0 };
  int[] dc = { 0, 0, -1, 1 };

      // Process all connected pixels
        while (q.Count > 0)
            {
   var (r, c) = q.Dequeue();
 count++;

      // Track maximum pressure in this blob
       if (matrix[r][c] > maxP) maxP = matrix[r][c];

   // Check all 4 neighbors
   for (int i = 0; i < 4; i++)
  {
        int nr = r + dr[i];
    int nc = c + dc[i];

   // Add neighbor if within bounds, not visited, and above threshold
   if (nr >= 0 && nr < MATRIX_SIZE && nc >= 0 && nc < MATRIX_SIZE)
  {
        if (!visited[nr, nc] && matrix[nr][nc] > threshold)
              {
   visited[nr, nc] = true;
       q.Enqueue((nr, nc));
       }
        }
         }
     }

  return (count, maxP);
        }
  }
}