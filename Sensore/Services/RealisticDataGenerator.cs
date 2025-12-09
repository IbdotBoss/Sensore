namespace Sensore.Services
{
    // Generates realistic-looking pressure sensor data for testing and demonstration.
    // Simulates human body pressure patterns with natural variations.
    // Used for creating sample data when real sensor hardware is not available.
    public static class RealisticDataGenerator
    {
        // Generates a 32x32 pressure matrix simulating a seated human.
        // Creates two Gaussian blobs representing left and right buttock contact.
        // Includes subtle movement simulation (breathing artifact).
        // param: timeStep - Time offset for simulating movement over time
        // returns: 32x32 array of pressure values (0-255)
        public static int[][] GenerateHumanShape(int timeStep)
        {
            int size = 32;
            
            // Initialize empty matrix
            int[][] matrix = new int[size][];
            for (int i = 0; i < size; i++) matrix[i] = new int[size];

            // Define center points for left and right pressure blobs
            // These simulate the typical seated pressure pattern
            double leftX = 10.0;   // Left buttock center
            double rightX = 22.0;  // Right buttock center
            double centerY = 16.0; // Vertical center

            // Add subtle oscillation to simulate breathing/movement
            // Creates natural variation in the data over time
            double shift = Math.Sin(timeStep * 0.1) * 1.5;

            // Generate pressure blobs using Gaussian distribution
            // Left blob with slight upward shift, right blob with downward shift
            AddGaussianBlob(matrix, leftX, centerY + shift, 5.0, 180);  // Left side
            AddGaussianBlob(matrix, rightX, centerY - shift, 5.0, 170); // Right side

            return matrix;
        }

        // Adds a Gaussian (bell curve) shaped pressure blob to the matrix.
        // Creates a natural-looking circular pressure distribution.
        // param: matrix - The matrix to add the blob to
        // param: cx - Center X position of the blob
        // param: cy - Center Y position of the blob
        // param: sigma - Spread of the blob (larger = wider distribution)
        // param: peakValue - Maximum pressure value at the center
        private static void AddGaussianBlob(int[][] matrix, double cx, double cy, double sigma, int peakValue)
        {
            for (int r = 0; r < 32; r++)
            {
                for (int c = 0; c < 32; c++)
                {
                    // Calculate squared distance from center
                    double distSq = Math.Pow(r - cy, 2) + Math.Pow(c - cx, 2);
 
                    // Apply Gaussian formula: value = peak * e^(-distance²/2σ²)
                    double exponent = -distSq / (2 * sigma * sigma);
                    double value = peakValue * Math.Exp(exponent);

                    // Accumulate value (allows overlapping blobs)
                    // Cap at 255 to stay within valid range
                    matrix[r][c] = Math.Min(255, matrix[r][c] + (int)value);

                    // Clean up noise floor - values below 5 are set to 0
                    // Creates cleaner blob boundaries
                    if (matrix[r][c] < 5) matrix[r][c] = 0;
                }
            }
        }
    }
}