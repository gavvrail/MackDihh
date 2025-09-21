using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.Hubs;

// --- BUILDER CREATION ---
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// --- SERVICE CONFIGURATION ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
    {
        options.SignIn.RequireConfirmedAccount = false;
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;
        
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        
        // User settings
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddTokenProvider<CustomSmsTokenProvider<ApplicationUser>>("Phone");

// Configure authentication cookies for Remember Me functionality
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Remember me duration
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true; // Reset expiration on activity
    
    // Security settings - Fixed for development
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.None 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add custom services
builder.Services.AddScoped<LoginSecurityService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<RecaptchaService>();

// Add background services
builder.Services.AddHostedService<OrderStatusUpdateService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddHttpContextAccessor();

// Configure SignalR for real-time chat
builder.Services.AddSignalR();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// --- APP CREATION ---
var app = builder.Build();

// --- SEEDING THE DATABASE ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        SeedData.Initialize(services);
        IdentityDataSeeder.Initialize(services).Wait();
        
        // Seed auto-responses
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await AutoResponseSeeder.SeedAutoResponsesAsync(context, userManager);
        
        // Unblock any admin accounts that might be blocked
        var loginSecurityService = services.GetRequiredService<LoginSecurityService>();
        await loginSecurityService.UnblockAllAdminAccountsAsync();
        
        // Seed sample reviews
        await ReviewSeeder.SeedReviews(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// --- HTTP PIPELINE CONFIGURATION ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    if (context.Request.IsHttps)
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();