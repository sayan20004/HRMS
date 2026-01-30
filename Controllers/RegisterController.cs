using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using HRMS.Services;
using System.Text.Json;
using System.Text;
using System.Security.Claims; // Needed for Claims
using Microsoft.AspNetCore.Authentication; // Needed for SignInAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for Cookie Scheme

namespace HRMS.Controllers
{
    public class RegisterController : Controller
    {
        private readonly HttpClient _client;
        private readonly GoogleCaptchaService _captchaService;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl = "http://localhost:5173/api/auth";

        public RegisterController(GoogleCaptchaService captchaService, IConfiguration configuration)
        {
            _client = new HttpClient();
            _captchaService = captchaService;
            _configuration = configuration;
        }

        // --- LOGIN PAGE ---
        public IActionResult Login()
        {
            // Check if User is already authenticated via Cookie
            if (User.Identity!.IsAuthenticated)
            {
                // Restore Session from Cookie Claims if needed (optional, but good for your existing code)
                var token = User.FindFirst("Token")?.Value;
                if (!string.IsNullOrEmpty(token)) HttpContext.Session.SetString("Token", token);
                
                return RedirectToAction("Index", "Dashboard");
            }
            
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
            return View("~/Views/authView/Login/Login.cshtml");
        }

        // --- LOGIN POST ---
        [HttpPost]
        public async Task<IActionResult> LoginPost(string email, string password, bool rememberMe = false)
        {
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];

            // ... (Captcha Logic - Keep existing) ...
             string captchaToken = Request.Form["g-recaptcha-response"].ToString() ?? "";
            if (string.IsNullOrEmpty(captchaToken) || !await _captchaService.VerifyToken(captchaToken))
            {
                ViewData["Error"] = "Security check failed.";
                return View("~/Views/authView/Login/Login.cshtml");
            }

            var loginModel = new { Email = email, Password = password }; 
            var content = new StringContent(JsonSerializer.Serialize(loginModel), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiBaseUrl}/login", content);

            if (response.IsSuccessStatusCode)
            {
                HttpContext.Session.SetString("PendingEmail", email);
                HttpContext.Session.SetString("RememberMe", rememberMe.ToString()); // Store choice for next step

                TempData["Message"] = "OTP sent to your email.";
                return RedirectToAction("VerifyLoginOtp");
            }
            
            ViewData["Error"] = "Invalid email or password.";
            return View("~/Views/authView/Login/Login.cshtml");
        }

        // --- VERIFY OTP (Where we create the Cookie) ---
        [HttpPost]
        public async Task<IActionResult> VerifyLoginOtp(VerifyOtpViewModel model)
        {
            var email = HttpContext.Session.GetString("PendingEmail");
            var rememberMe = HttpContext.Session.GetString("RememberMe") == "True";

            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var payload = new { Email = email, Otp = model.Otp, RememberMe = rememberMe };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiBaseUrl}/verify-login-otp", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var authData = JsonSerializer.Deserialize<AuthResponse>(responseString, options);

                if (authData != null)
                {
                    // 1. CREATE CLAIMS (Store Token inside the User Identity)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, authData.FullName),
                        new Claim(ClaimTypes.Email, authData.Email),
                        new Claim("Token", authData.Token) // Store JWT here
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        // 2. SET PERSISTENCE BASED ON 'REMEMBER ME'
                        IsPersistent = rememberMe, 
                        ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(60)
                    };

                    // 3. SIGN IN (Creates the encrypted cookie)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 4. Also set Session for backward compatibility with your other controllers
                    HttpContext.Session.SetString("Token", authData.Token);
                    HttpContext.Session.SetString("Username", authData.FullName);
                    HttpContext.Session.SetString("Email", authData.Email);

                    // Cleanup
                    HttpContext.Session.Remove("PendingEmail");
                    HttpContext.Session.Remove("RememberMe");

                    return RedirectToAction("Index", "Dashboard");
                }
            }

            ModelState.AddModelError("", "Invalid OTP.");
            return View("~/Views/authView/Login/VerifyLoginOtp.cshtml", model);
        }

        // --- LOGOUT ---
        public async Task<IActionResult> Logout()
        {
            // Sign Out from Cookie Auth
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Clear Session
            HttpContext.Session.Clear();
            
            return RedirectToAction("Login");
        }

        // ... (Keep other methods like Register, ForgotPassword as they are) ...
        // Need to add back VerifyLoginOtp GET method if missing
        [HttpGet]
        public IActionResult VerifyLoginOtp()
        {
             if (string.IsNullOrEmpty(HttpContext.Session.GetString("PendingEmail"))) return RedirectToAction("Login");
             return View("~/Views/authView/Login/VerifyLoginOtp.cshtml");
        }
         public IActionResult Index()
        {
             if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
             ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
             return View("~/Views/authView/Register/Register.cshtml");
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // ... (Keep existing Register logic) ...
            // (Just copy paste your existing Register logic here, no changes needed for Login Cookie)
             ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
            if (!ModelState.IsValid) return View("~/Views/authView/Register/Register.cshtml", model);
             string captchaToken = Request.Form["g-recaptcha-response"].ToString() ?? "";
            if (string.IsNullOrEmpty(captchaToken) || !await _captchaService.VerifyToken(captchaToken))
            {
                ModelState.AddModelError("", "Security check failed.");
                return View("~/Views/authView/Register/Register.cshtml", model);
            }
             bool rememberMe = Request.Form["rememberMe"] == "true";
             var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
             try
            {
                var response = await _client.PostAsync($"{_apiBaseUrl}/register", content);
                if (response.IsSuccessStatusCode)
                {
                    HttpContext.Session.SetString("PendingEmail", model.Email);
                    HttpContext.Session.SetString("RememberMe", rememberMe.ToString());
                    TempData["Message"] = "Verification passed! OTP sent to your email.";
                    return RedirectToAction("VerifyRegisterOtp");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", "Registration Failed: " + error);
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Error connecting to API.");
            }
            return View("~/Views/authView/Register/Register.cshtml", model);
        }

        [HttpGet]
        public IActionResult VerifyRegisterOtp()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("PendingEmail")))
                return RedirectToAction("Index");
            return View("~/Views/authView/Register/VerifyRegisterOtp.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> VerifyRegisterOtp(VerifyOtpViewModel model)
        {
            var email = HttpContext.Session.GetString("PendingEmail");
            var rememberMe = HttpContext.Session.GetString("RememberMe") == "True";

            if (string.IsNullOrEmpty(email)) return RedirectToAction("Index");

            var payload = new { Email = email, Otp = model.Otp, RememberMe = rememberMe };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiBaseUrl}/verify-register-otp", content);

            if (response.IsSuccessStatusCode)
            {
                HttpContext.Session.Remove("PendingEmail");
                HttpContext.Session.Remove("RememberMe");
                TempData["Success"] = "Email Verified! You can now Login.";
                return RedirectToAction("Login");
            }
            ModelState.AddModelError("", "Invalid OTP. Please try again.");
            return View("~/Views/authView/Register/VerifyRegisterOtp.cshtml", model);
        }
         public IActionResult ForgotPassword()
        {
             return View("~/Views/authView/ForgotPassword/ForgotPassword.cshtml");
        }
    }
}