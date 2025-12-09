using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

// ============================================================================
// Sensore Application - Main Entry Point
// A medical pressure monitoring system for patient care management.
// This file configures services, middleware, and database seeding.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// DATABASE CONFIGURATION
// Configure Entity Framework with SQL Server connection
// ----------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----------------------------------------------------------------------------
// IDENTITY CONFIGURATION
// Set up ASP.NET Core Identity for authentication and authorization
// Supports three roles: Admin, Clinician, and Patient
// ----------------------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
    // Password complexity requirements for security
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // Account lockout settings to prevent brute force attacks
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User account settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true if using email confirmation
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); // Adds the default Identity Razor Pages UI

// ----------------------------------------------------------------------------
// APPLICATION SERVICES
// Register custom services for pressure analysis, reporting, and data import
// ----------------------------------------------------------------------------
builder.Services.AddScoped<Sensore.Services.PressureAnalysisService>();
builder.Services.AddScoped<Sensore.Services.ReportingService>();
builder.Services.AddScoped<Sensore.Services.CsvIngestionService>();

// Add MVC and Razor Pages support
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Required for Identity UI

var app = builder.Build();

// ----------------------------------------------------------------------------
// DATABASE SEEDING
// Initialize the database with roles, admin user, and sample patient data
// This runs on application startup
// ----------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
     Console.WriteLine("\n========================================");
        Console.WriteLine("?? Starting Database Seeding Process...");
        Console.WriteLine("========================================\n");

      var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        
        // Create roles (Admin, Clinician, Patient) and default admin user
  await Sensore.Data.DbInitializer.Initialize(services, userManager, roleManager);

        // Seed sample patient data from CSV files for demonstration
        await Sensore.Data.SeedData.Initialize(services);
        
        Console.WriteLine("\n========================================");
        Console.WriteLine("? Database Seeding Completed!");
        Console.WriteLine("========================================\n");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
   logger.LogError(ex, "? An error occurred while seeding the database.");
        Console.WriteLine($"\n? Seeding Error: {ex.Message}");
  Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    }
}

// ----------------------------------------------------------------------------
// HTTP REQUEST PIPELINE CONFIGURATION
// Configure middleware for request handling
// ----------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    // Use custom error handler and HSTS in production
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Serve static files (CSS, JS, images)
app.MapStaticAssets();

// Configure MVC routing with default route pattern
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map Razor Pages for Identity UI
app.MapRazorPages();

// Start the application
app.Run();
