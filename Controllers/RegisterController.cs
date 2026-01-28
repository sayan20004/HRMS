using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace HRMS.Controllers
{
    public class RegisterController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "http://localhost:5173/api";
        
        public RegisterController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        
        public IActionResult Index()
        {
            ViewData["Title"] = "Registration Page";
            return View("~/Views/authView/Register/Register.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel? model)
        {
            try
            {
                if (model == null)
                {
                    ModelState.AddModelError("", "Invalid form submission. Please try again.");
                    return View("~/Views/authView/Register/Register.cshtml", new RegisterViewModel());
                }
                
                if (!ModelState.IsValid)
                {
                    return View("~/Views/authView/Register/Register.cshtml", model);
                }
                
                // Call API to register user
                var client = _httpClientFactory.CreateClient();
                var registerDto = new
                {
                    email = model.Email,
                    password = model.Password,
                    fullName = model.FullName
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(registerDto),
                    Encoding.UTF8,
                    "application/json"
                );
                
                var response = await client.PostAsync($"{_apiBaseUrl}/Auth/register", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Set session
                    HttpContext.Session.SetString("Username", model.FullName ?? "User");
                    HttpContext.Session.SetString("Email", model.Email ?? "");
                    
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", errorContent);
                    return View("~/Views/authView/Register/Register.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View("~/Views/authView/Register/Register.cshtml", model ?? new RegisterViewModel());
            }
        }

        public IActionResult Login()
        {
            if (Request.Cookies.TryGetValue("RememberMe_Email", out var email) && 
                Request.Cookies.TryGetValue("RememberMe_Username", out var username))
            {
                HttpContext.Session.SetString("Username", username);
                HttpContext.Session.SetString("Email", email);
                return RedirectToAction("Index", "Dashboard");
            }
            
            ViewData["Title"] = "Login Page";
            return View("~/Views/authView/Login/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> LoginPost(string email, string password, bool rememberMe = false)
        {
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                try
                {
                    // Call API to login
                    var client = _httpClientFactory.CreateClient();
                    var loginDto = new { email, password };
                    
                    var content = new StringContent(
                        JsonSerializer.Serialize(loginDto),
                        Encoding.UTF8,
                        "application/json"
                    );
                    
                    var response = await client.PostAsync($"{_apiBaseUrl}/Auth/login", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        // Set session
                        HttpContext.Session.SetString("Username", email.Split('@')[0]);
                        HttpContext.Session.SetString("Email", email);
                        HttpContext.Session.SetString("Token", loginResponse.GetProperty("token").GetString() ?? "");
                        
                        if (rememberMe)
                        {
                            var cookieOptions = new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddDays(30),
                                HttpOnly = true,
                                Secure = false, // Changed to false for HTTP
                                SameSite = SameSiteMode.Lax
                            };
                            
                            Response.Cookies.Append("RememberMe_Email", email, cookieOptions);
                            Response.Cookies.Append("RememberMe_Username", email.Split('@')[0], cookieOptions);
                        }
                        
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        ViewData["Error"] = "Invalid email or password";
                        return View("~/Views/authView/Login/Login.cshtml");
                    }
                }
                catch (Exception ex)
                {
                    ViewData["Error"] = $"Login failed: {ex.Message}";
                    return View("~/Views/authView/Login/Login.cshtml");
                }
            }
            
            ViewData["Error"] = "Invalid email or password";
            return View("~/Views/authView/Login/Login.cshtml");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewData["Title"] = "Forgot Password";
            return View("~/Views/authView/ForgotPassword/ForgotPassword.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewData["Error"] = "Please enter your email address";
                return View("~/Views/authView/ForgotPassword/ForgotPassword.cshtml");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var forgotPasswordDto = new { Email = email };

                var content = new StringContent(
                    JsonSerializer.Serialize(forgotPasswordDto),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/Auth/forgot-password", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    ViewData["Success"] = "If the email exists, a password reset link has been sent.";
                }
                else
                {
                    ViewData["Error"] = $"Error: {response.StatusCode} - {responseContent}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                ViewData["Error"] = $"An error occurred: {ex.Message}";
            }

            return View("~/Views/authView/ForgotPassword/ForgotPassword.cshtml");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ViewData["Error"] = "Invalid reset link";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            ViewData["Title"] = "Reset Password";
            return View("~/Views/authView/ResetPassword/ResetPassword.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/authView/ResetPassword/ResetPassword.cshtml", model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(model),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/Auth/reset-password", content);

                if (response.IsSuccessStatusCode)
                {
                    ViewData["Success"] = "Password reset successful! You can now login.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewData["Error"] = "Failed to reset password. The link may have expired.";
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = $"An error occurred: {ex.Message}";
            }

            return View("~/Views/authView/ResetPassword/ResetPassword.cshtml", model);
        }
    }
}