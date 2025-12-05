using System.Text.Json;

namespace Sensore.Services
{
    public class PressureAnalysisService
    {
        // Configuration constants (could be injected from options)
        private const int MATRIX_SIZE = 32;

        public struct AnalysisResult
        {
            public int PeakPressure { get; set; }
            public double ContactAreaPercent { get; set; }
            public bool IsAlertFlagged { get; set; }
        }

        public AnalysisResult AnalyzeFrame(int[][] matrix, int highThreshold, int minBlobSize, int contactThreshold)
        {
            var result = new AnalysisResult();

            // 1. Calculate Contact Area %
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

            // 2. Blob Detection (Connected Component Labeling) for Peak Pressure
            // We only consider "valid" blobs (size >= minBlobSize)
            var visited = new bool[MATRIX_SIZE, MATRIX_SIZE];
            int maxPressureInValidBlobs = 0;
            bool alertTriggered = false;

            for (int r = 0; r < MATRIX_SIZE; r++)
            {
                for (int c = 0; c < MATRIX_SIZE; c++)
                {
                    // If pixel has pressure and hasn't been visited
                    if (matrix[r][c] > contactThreshold && !visited[r, c])
                    {
                        // Perform BFS to find the whole blob
                        var blob = GetBlob(matrix, r, c, visited, contactThreshold);

                        // Requirement 4a: Exclude areas less than minBlobSize (e.g., 10 pixels)
                        if (blob.PixelCount >= minBlobSize)
                        {
                            if (blob.MaxPressure > maxPressureInValidBlobs)
                            {
                                maxPressureInValidBlobs = blob.MaxPressure;
                            }

                            // Requirement 3: Check alert condition
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

        private (int PixelCount, int MaxPressure) GetBlob(int[][] matrix, int startR, int startC, bool[,] visited, int threshold)
        {
            var q = new Queue<(int r, int c)>();
            q.Enqueue((startR, startC));
            visited[startR, startC] = true;

            int count = 0;
            int maxP = 0;

            int[] dr = { -1, 1, 0, 0 }; // Up, Down, Left, Right (4-connectivity)
            int[] dc = { 0, 0, -1, 1 };

            while (q.Count > 0)
            {
                var (r, c) = q.Dequeue();
                count++;
                if (matrix[r][c] > maxP) maxP = matrix[r][c];

                for (int i = 0; i < 4; i++)
                {
                    int nr = r + dr[i];
                    int nc = c + dc[i];

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