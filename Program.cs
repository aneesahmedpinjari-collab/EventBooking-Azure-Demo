using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EventBookingSecure.Data;
using EventBookingSecure.Models;
using EventBookingSecure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("MySQL connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Google Authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

// Register URL Validator Service
builder.Services.AddScoped<IUrlValidator, UrlValidator>();

// Configure Cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Seed database with roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        await SeedRoles(services);
        var adminUser = await SeedAdminUser(services);
        await SeedSampleEvents(dbContext, adminUser);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ============================================
// HELPER METHODS: Seed Roles
// ============================================
static async Task SeedRoles(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames = { "Admin", "Organizer", "User" };

    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

// ============================================
// HELPER METHODS: Seed Admin User
// ============================================
static async Task<ApplicationUser> SeedAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string adminEmail = "admin@eventbooking.com";
    string adminPassword = "Admin@123";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var newAdmin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newAdmin, adminPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to create the default admin user.");
        }

        await userManager.AddToRoleAsync(newAdmin, "Admin");
        adminUser = newAdmin;
    }
    else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    return adminUser;
}

static async Task SeedSampleEvents(ApplicationDbContext context, ApplicationUser organizer)
{
    if (context.Events.Any())
    {
        return;
    }

    var now = DateTime.UtcNow;
    var sampleEvents = new[]
    {
        new Event
        {
            Title = "Secure Cloud Summit",
            Description = "Dive into the latest in zero trust networking and resilient architectures.",
            EventDate = now.AddDays(7),
            Location = "Seattle, USA",
            Capacity = 250,
            AvailableSeats = 250,
            Price = 199,
            OrganizerId = organizer.Id,
            ImageUrl = "https://images.unsplash.com/photo-1545239351-1141bd82e8a6"
        },
        new Event
        {
            Title = "AI for Good Hackathon",
            Description = "Build accessible civic tech with a global community of makers.",
            EventDate = now.AddDays(12),
            Location = "Remote",
            Capacity = 150,
            AvailableSeats = 150,
            Price = 0,
            OrganizerId = organizer.Id,
            ImageUrl = "https://images.unsplash.com/photo-1518779578993-ec3579fee39f"
        },
        new Event
        {
            Title = "Green Energy Expo",
            Description = "Showcase of sustainable energy startups and policy discussions.",
            EventDate = now.AddDays(20),
            Location = "Copenhagen, Denmark",
            Capacity = 500,
            AvailableSeats = 500,
            Price = 299,
            OrganizerId = organizer.Id,
            ImageUrl = "https://images.unsplash.com/photo-1489515217757-5fd1be406fef"
        }
    };

    context.Events.AddRange(sampleEvents);
    await context.SaveChangesAsync();
}
