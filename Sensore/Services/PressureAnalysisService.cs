using System.Text.Json;

namespace Sensore.Services
{
    // Analyzes 32x32 pressure sensor matrices to detect high-pressure areas
    // and determine if an alert should be triggered for the patient.
    public class PressureAnalysisService
    {
        private const int MATRIX_SIZE = 32;

        public struct AnalysisResult
        {
   public int PeakPressure { get; set; }
     public double ContactAreaPercent { get; set; }
            public bool IsAlertFlagged { get; set; }
     }

        // Processes a pressure matrix and calculates key metrics.
    // Returns the peak pressure, contact area percentage, and whether
        // the frame should trigger an alert based on the threshold settings.
        public AnalysisResult AnalyzeFrame(int[][] matrix, int highThreshold, int minBlobSize, int contactThreshold)
        {
          if (matrix == null || matrix.Length != MATRIX_SIZE)
                throw new ArgumentException("Invalid matrix size");

            var result = new AnalysisResult();

     // Count pixels above contact threshold for area percentage
 int contactPixels = 0;
    for (int r = 0; r < MATRIX_SIZE; r++)
             for (int c = 0; c < MATRIX_SIZE; c++)
  if (matrix[r][c] > contactThreshold)
        contactPixels++;

            result.ContactAreaPercent = Math.Round(contactPixels / 1024.0 * 100, 2);

      // Find connected regions (blobs) using flood fill
     var visited = new bool[MATRIX_SIZE, MATRIX_SIZE];
       int maxPressure = 0;
     bool alertTriggered = false;

          for (int r = 0; r < MATRIX_SIZE; r++)
            {
        for (int c = 0; c < MATRIX_SIZE; c++)
         {
          if (matrix[r][c] > contactThreshold && !visited[r, c])
         {
     var blob = GetBlob(matrix, r, c, visited, contactThreshold);
       
         // Only consider blobs large enough (filters out noise)
 if (blob.Count >= minBlobSize)
       {
           if (blob.MaxP > maxPressure) maxPressure = blob.MaxP;
          if (blob.MaxP > highThreshold) alertTriggered = true;
       }
    }
}
            }

    result.PeakPressure = maxPressure;
  result.IsAlertFlagged = alertTriggered;
   return result;
  }

        // Finds all connected pixels forming a pressure blob using BFS.
        // Returns the blob size and maximum pressure value within it.
        private (int Count, int MaxP) GetBlob(int[][] matrix, int startR, int startC, bool[,] visited, int threshold)
        {
    var q = new Queue<(int r, int c)>();
            q.Enqueue((startR, startC));
            visited[startR, startC] = true;

      int count = 0, maxP = 0;
            int[] dr = { -1, 1, 0, 0 }, dc = { 0, 0, -1, 1 };

  while (q.Count > 0)
 {
        var (r, c) = q.Dequeue();
     count++;
        if (matrix[r][c] > maxP) maxP = matrix[r][c];

          // Check all 4 adjacent pixels
        for (int i = 0; i < 4; i++)
      {
   int nr = r + dr[i], nc = c + dc[i];
           if (nr >= 0 && nr < MATRIX_SIZE && nc >= 0 && nc < MATRIX_SIZE &&
                 !visited[nr, nc] && matrix[nr][nc] > threshold)
     {
         visited[nr, nc] = true;
              q.Enqueue((nr, nc));
           }
        }
            }
     return (count, maxP);
        }
    }
}