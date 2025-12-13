namespace Sensore.Models
{
    // View model for the error page.
    // Displays error information when something goes wrong.
    public class ErrorViewModel
    {
        // The unique identifier for this request, used for troubleshooting.
        public string? RequestId { get; set; }

        // Determines if the RequestId should be displayed.
        // Only shows when a valid RequestId exists.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
