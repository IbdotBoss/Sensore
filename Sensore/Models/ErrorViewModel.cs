namespace Sensore.Models
{
    // View model for error pages with request tracking.
    public class ErrorViewModel
    {
        // Unique request identifier for troubleshooting.
        public string? RequestId { get; set; }

        // True if RequestId should be displayed (not null/empty).
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
