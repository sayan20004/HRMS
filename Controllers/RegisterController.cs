using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using System.Text.Json;
using System.Text;

namespace HRMS.Controllers
{
    public class RegisterController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBaseUrl = "http://localhost:5173/api/auth";

        public RegisterController()
        {
            _client = new HttpClient();
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Token") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            ViewData["Title"] = "Registration Page";
            return View("~/Views/authView/Register/Register.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/authView/Register/Register.cshtml", model);
            }

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await _client.PostAsync($"{_apiBaseUrl}/register", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }
                else
                {
                    // Read error from API
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", "Registration failed: " + errorContent);
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Unable to connect to the server.");
            }

            return View("~/Views/authView/Register/Register.cshtml", model);
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Token") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            ViewData["Title"] = "Login Page";
            return View("~/Views/authView/Login/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> LoginPost(string email, string password, bool rememberMe = false)
        {
            var loginModel = new { Email = email, Password = password, RememberMe = rememberMe };
            var json = JsonSerializer.Serialize(loginModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PostAsync($"{_apiBaseUrl}/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var authData = JsonSerializer.Deserialize<AuthResponse>(responseString, options);

                    if (authData != null)
                    {
                        // Save Token to Session
                        HttpContext.Session.SetString("Token", authData.Token);
                        HttpContext.Session.SetString("Username", authData.FullName);
                        HttpContext.Session.SetString("Email", authData.Email);
                        
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
                
                ViewData["Error"] = "Invalid email or password";
            }
            catch (Exception)
            {
                ViewData["Error"] = "Unable to connect to server.";
            }

            return View("~/Views/authView/Login/Login.cshtml");
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