using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sensore.Models;
using System.Security.Claims;

namespace Sensore.Controllers
{
    // Handles the public-facing pages of the application.
    // Redirects authenticated users to their role-specific dashboards.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Displays the home page or redirects to the appropriate dashboard.
        // Authenticated users are sent to their role-specific dashboard:
        // - Admin -> Admin dashboard
        // - Clinician -> Clinician dashboard
        // - Patient -> Patient dashboard
        public IActionResult Index()
        {
            // Redirect authenticated users to their dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (User.IsInRole("Clinician"))
                {
                    return RedirectToAction("Index", "Clinician");
                }
                else if (User.IsInRole("Patient"))
                {
                    return RedirectToAction("Dashboard", "Patient");
                }
            }

            // Show landing page for unauthenticated users
            return View();
        }

        // Displays the privacy policy page.
        public IActionResult Privacy()
        {
            return View();
        }

        // Displays the error page when something goes wrong.
        // Caching is disabled to ensure fresh error information.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
