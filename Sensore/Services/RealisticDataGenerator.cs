namespace Sensore.Services
{
    // Creates realistic pressure sensor data for testing and demos.
    // Simulates a person sitting with two main pressure points
    // that shift slightly over time to mimic natural movement.
    public static class RealisticDataGenerator
    {
        // Generates a 32x32 pressure matrix simulating a seated person.
        // The timeStep parameter creates subtle variations between frames
        // to simulate breathing and small position adjustments.
        public static int[][] GenerateHumanShape(int timeStep)
        {
            int size = 32;
            
            int[][] matrix = new int[size][];
            for (int i = 0; i < size; i++) matrix[i] = new int[size];

            // Two pressure blobs representing left and right contact points
            double leftX = 10.0;
            double rightX = 22.0;
            double centerY = 16.0;

            // Subtle movement to make data look natural over time
            double shift = Math.Sin(timeStep * 0.1) * 1.5;

            AddGaussianBlob(matrix, leftX, centerY + shift, 5.0, 180);
            AddGaussianBlob(matrix, rightX, centerY - shift, 5.0, 170);

            return matrix;
        }

        // Creates a bell-curve shaped pressure area on the matrix.
        // Pressure is highest at the center and fades toward edges.
        private static void AddGaussianBlob(int[][] matrix, double cx, double cy, double sigma, int peakValue)
        {
            for (int r = 0; r < 32; r++)
            {
                for (int c = 0; c < 32; c++)
                {
                    double distSq = Math.Pow(r - cy, 2) + Math.Pow(c - cx, 2);
                    double exponent = -distSq / (2 * sigma * sigma);
                    double value = peakValue * Math.Exp(exponent);

                    // Add to existing value (blobs can overlap)
                    matrix[r][c] = Math.Min(255, matrix[r][c] + (int)value);

                    // Remove noise floor for cleaner boundaries
                    if (matrix[r][c] < 5) matrix[r][c] = 0;
                }
            }
        }
    }
}