using HRMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies; // Add this namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Add Distributed Memory Cache (for Session)
builder.Services.AddDistributedMemoryCache();

// 2. Add Session (Keep this for temporary data like OTP flow)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. ADD COOKIE AUTHENTICATION (Crucial for "Remember Me")
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Register/Login"; // Redirect here if not logged in
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Default cookie life
        options.Cookie.Name = "HRMS_Auth_Cookie";
    });

// 4. Register Google Captcha Service
builder.Services.AddHttpClient<GoogleCaptchaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5. ENABLE AUTHENTICATION & AUTHORIZATION
app.UseAuthentication(); // Must be before Authorization
app.UseAuthorization();

app.UseSession(); // Must be after UseRouting

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Register}/{action=Login}/{id?}"); // Default to Login

app.Run();