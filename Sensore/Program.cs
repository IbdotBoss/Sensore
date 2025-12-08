using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true if using email confirmation
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); // This adds the default Identity Razor Pages UI

// Register application services
builder.Services.AddScoped<Sensore.Services.PressureAnalysisService>();
builder.Services.AddScoped<Sensore.Services.ReportingService>();
builder.Services.AddScoped<Sensore.Services.CsvIngestionService>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Required for Identity UI

var app = builder.Build();

// Seed the Database on Startup
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
        
        // Initialize roles and admin user
        await Sensore.Data.DbInitializer.Initialize(services, userManager, roleManager);
        
        // Seed patient data from CSV files
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
