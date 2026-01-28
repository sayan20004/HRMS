using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace HRMS.Controllers
{
    public class ProfileController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBaseUrl = "http://localhost:5173/api/auth";

        public ProfileController()
        {
            _client = new HttpClient();
        }

        private void AddAuthHeader()
        {
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<IActionResult> Index()
        {
            // Check if token exists
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return Content("DEBUG ERROR: No Token found in Session. Please Login again.");
            }

            AddAuthHeader();
            var response = await _client.GetAsync($"{_apiBaseUrl}/profile");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var profile = JsonSerializer.Deserialize<ProfileViewModel>(content, options);
                return View(profile);
            }
            
            
            return Content($"DEBUG ERROR: API Call Failed. Status Code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            AddAuthHeader();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_apiBaseUrl}/profile", content);

            if (response.IsSuccessStatusCode)
            {
                HttpContext.Session.SetString("Username", model.FullName);
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }
            return View("Index", model);
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            AddAuthHeader();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_apiBaseUrl}/change-password", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to change password. Check your current password.");
            return View(model);
        }
    }
}