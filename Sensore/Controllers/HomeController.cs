using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sensore.Models;
using System.Security.Claims;

namespace Sensore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // If user is authenticated, redirect to appropriate dashboard based on role
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

            // If not authenticated, show the home page
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
