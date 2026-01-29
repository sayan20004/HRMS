using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using HRMS.Services;
using System.Text.Json;
using System.Text;

namespace HRMS.Controllers
{
    public class RegisterController : Controller
    {
        private readonly HttpClient _client;
        private readonly GoogleCaptchaService _captchaService;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl = "http://localhost:5173/api/auth"; // Ensure this matches your API port

        public RegisterController(GoogleCaptchaService captchaService, IConfiguration configuration)
        {
            _client = new HttpClient();
            _captchaService = captchaService;
            _configuration = configuration;
        }

        // --- REGISTER: Step 1 (Form) ---
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Token") != null)
                return RedirectToAction("Index", "Dashboard");

            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
            return View("~/Views/authView/Register/Register.cshtml");
        }

        // --- REGISTER: Step 1 (Submit) ---
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];

            if (!ModelState.IsValid) return View("~/Views/authView/Register/Register.cshtml", model);

            // 1. CAPTCHA CHECK
            string captchaToken = Request.Form["g-recaptcha-response"].ToString() ?? "";
            if (string.IsNullOrEmpty(captchaToken) || !await _captchaService.VerifyToken(captchaToken))
            {
                ModelState.AddModelError("", "Security check failed. You appear to be automated.");
                return View("~/Views/authView/Register/Register.cshtml", model);
            }

            // 2. CAPTURE 'REMEMBER ME' (Manually since it's not in RegisterViewModel)
            bool rememberMe = Request.Form["rememberMe"] == "true";

            // 3. SEND API REQUEST
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await _client.PostAsync($"{_apiBaseUrl}/register", content);

                if (response.IsSuccessStatusCode)
                {
                    // Store Email & RememberMe in Session for the next step (OTP)
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

        // --- REGISTER:  (OTP Page) ---
        [HttpGet]
        public IActionResult VerifyRegisterOtp()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("PendingEmail")))
                return RedirectToAction("Index");
            return View("~/Views/authView/Register/VerifyRegisterOtp.cshtml");
        }

        // --- REGISTER:(OTP Verify) ---
        [HttpPost]
        public async Task<IActionResult> VerifyRegisterOtp(VerifyOtpViewModel model)
        {
            var email = HttpContext.Session.GetString("PendingEmail");
            var rememberMe = HttpContext.Session.GetString("RememberMe") == "True";

            if (string.IsNullOrEmpty(email)) return RedirectToAction("Index");

            // Include 'RememberMe' in the payload to the API
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

        // --- LOGIN: Step 1 (Form) ---
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Token") != null)
                return RedirectToAction("Index", "Dashboard");

            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
            return View("~/Views/authView/Login/Login.cshtml");
        }

        // --- LOGIN: Step 1 (Submit) ---
        [HttpPost]
        public async Task<IActionResult> LoginPost(string email, string password, bool rememberMe = false)
        {
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];

            // 1. CAPTCHA CHECK
            string captchaToken = Request.Form["g-recaptcha-response"].ToString() ?? "";
            if (string.IsNullOrEmpty(captchaToken) || !await _captchaService.VerifyToken(captchaToken))
            {
                ViewData["Error"] = "Security check failed. You appear to be automated.";
                return View("~/Views/authView/Login/Login.cshtml");
            }

            // 2. SEND API REQUEST
            var loginModel = new { Email = email, Password = password }; 
            var content = new StringContent(JsonSerializer.Serialize(loginModel), Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PostAsync($"{_apiBaseUrl}/login", content);

                if (response.IsSuccessStatusCode)
                {
                    // Store Email & RememberMe in Session for OTP step
                    HttpContext.Session.SetString("PendingEmail", email);
                    HttpContext.Session.SetString("RememberMe", rememberMe.ToString());

                    TempData["Message"] = "Credentials Valid. OTP sent to your email.";
                    return RedirectToAction("VerifyLoginOtp");
                }
                
                ViewData["Error"] = "Invalid email or password.";
            }
            catch (Exception)
            {
                ViewData["Error"] = "Error connecting to API.";
            }

            return View("~/Views/authView/Login/Login.cshtml");
        }

        // --- LOGIN: Step 2 (OTP Page) ---
        [HttpGet]
        public IActionResult VerifyLoginOtp()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("PendingEmail"))) return RedirectToAction("Login");
            return View("~/Views/authView/Login/VerifyLoginOtp.cshtml");
        }

        // --- LOGIN: Step 2 (OTP Verify) ---
        [HttpPost]
        public async Task<IActionResult> VerifyLoginOtp(VerifyOtpViewModel model)
        {
            var email = HttpContext.Session.GetString("PendingEmail");
            var rememberMe = HttpContext.Session.GetString("RememberMe") == "True";

            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            // Include 'RememberMe' in the payload
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
                    // LOGIN SUCCESS -> STORE TOKEN
                    HttpContext.Session.SetString("Token", authData.Token);
                    HttpContext.Session.SetString("Username", authData.FullName);
                    HttpContext.Session.SetString("Email", authData.Email);
                    
                    // CLEANUP
                    HttpContext.Session.Remove("PendingEmail");
                    HttpContext.Session.Remove("RememberMe");
                    
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            ModelState.AddModelError("", "Invalid OTP.");
            return View("~/Views/authView/Login/VerifyLoginOtp.cshtml", model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        
        public IActionResult ForgotPassword()
        {
             return View("~/Views/authView/ForgotPassword/ForgotPassword.cshtml");
        }
    }
}