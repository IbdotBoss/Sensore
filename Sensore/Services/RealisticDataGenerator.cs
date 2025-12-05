namespace Sensore.Services
{
    public static class RealisticDataGenerator
    {
        public static int[][] GenerateHumanShape(int timeStep)
        {
            int size = 32;
            int[][] matrix = new int[size][];
            for (int i = 0; i < size; i++) matrix[i] = new int[size];

            // Center points for "Left Buttock" and "Right Buttock"
            double leftX = 10.0;
            double rightX = 22.0;
            double centerY = 16.0;

            // Add slight movement (breathing artifact)
            double shift = Math.Sin(timeStep * 0.1) * 1.5;

            // Generate Gaussian Blobs
            AddGaussianBlob(matrix, leftX, centerY + shift, 5.0, 180); // Left
            AddGaussianBlob(matrix, rightX, centerY - shift, 5.0, 170); // Right

            return matrix;
        }

        private static void AddGaussianBlob(int[][] matrix, double cx, double cy, double sigma, int peakValue)
        {
            for (int r = 0; r < 32; r++)
            {
                for (int c = 0; c < 32; c++)
                {
                    double distSq = Math.Pow(r - cy, 2) + Math.Pow(c - cx, 2);
                    double exponent = -distSq / (2 * sigma * sigma);
                    double value = peakValue * Math.Exp(exponent);

                    // Add to existing value (accumulate)
                    matrix[r][c] = Math.Min(255, matrix[r][c] + (int)value);

                    // Noise floor cleaning
                    if (matrix[r][c] < 5) matrix[r][c] = 0;
                }
            }
        }
    }
}