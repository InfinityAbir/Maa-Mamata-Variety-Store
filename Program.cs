using Microsoft.EntityFrameworkCore;
using Ecommerce.Models; // your DbContext namespace

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<EcommerceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EcommerceDbContext")
    ?? throw new InvalidOperationException("Connection string 'EcommerceDbContext' not found.")));

// Add MVC
builder.Services.AddControllersWithViews();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable session before UseAuthorization
app.UseSession();

app.UseAuthorization();

// Updated default route: opens Home/Index first
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
