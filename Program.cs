using BarterSystem.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"=== CONNECTION STRING ===");
Console.WriteLine(connectionString);
Console.WriteLine($"==========================");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(300);
        sqlOptions.EnableRetryOnFailure();
    }));

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Database setup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Console.WriteLine("Testing database connection...");

        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ DATABASE CONNECTION SUCCESSFUL");
            context.Database.Migrate();
            Console.WriteLine("✅ MIGRATIONS APPLIED");

            // Ensure wwwroot/images directory exists
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var imagesPath = Path.Combine(webRootPath, "images");

            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
                Console.WriteLine("✅ Created wwwroot/images directory");
            }

            // Create default no-image.jpg
            var noImagePath = Path.Combine(imagesPath, "no-image.jpg");
            if (!File.Exists(noImagePath))
            {
                await File.WriteAllTextAsync(noImagePath, "placeholder");
                Console.WriteLine("✅ Created default no-image.jpg");
            }
        }
        else
        {
            Console.WriteLine("❌ DATABASE CONNECTION FAILED");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ DATABASE ERROR: {ex.Message}");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();