using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sensore.Models;
using System.Security.Claims;

namespace Sensore.Controllers
{
    // Handles the public pages like home and privacy.
    // Automatically redirects logged-in users to their dashboard.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
 _logger = logger;
   }

        // Shows the landing page for visitors, or redirects logged-in
    // users to their role-specific dashboard (Admin, Clinician, or Patient).
        public IActionResult Index()
        {
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

            return View();
        }

        // Shows the privacy policy page.
   public IActionResult Privacy()
        {
    return View();
        }

        // Shows the error page when something goes wrong.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
    {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
