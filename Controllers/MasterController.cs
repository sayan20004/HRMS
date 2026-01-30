using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace HRMS.Controllers
{
    public class MasterController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBaseUrl = "http://localhost:5173/api/master";

        public MasterController()
        {
            _client = new HttpClient();
        }

        private void AddAuthHeader()
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token) && User.Identity!.IsAuthenticated)
                token = User.FindFirst("Token")?.Value;

            if (!string.IsNullOrEmpty(token))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // --- 1. DEPARTMENT PAGE (Create + List) ---
        [HttpGet]
        public async Task<IActionResult> CreateDepartment()
        {
            AddAuthHeader();
            var model = new MasterViewModel();

            // Fetch Existing Departments to show below form
            var response = await _client.GetAsync($"{_apiBaseUrl}/departments");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                model.Departments = JsonSerializer.Deserialize<List<DepartmentViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DepartmentViewModel>();
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(MasterViewModel model)
        {
            AddAuthHeader();
            var data = new { Name = model.DeptName, Code = model.DeptCode }; 
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiBaseUrl}/departments", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Department Created Successfully";
            else
                TempData["Error"] = "Failed to create Department";

            return RedirectToAction("CreateDepartment"); // Reloads page (GET) which re-fetches list
        }

        // --- 2. DESIGNATION PAGE (Create + List) ---
        [HttpGet]
        public async Task<IActionResult> CreateDesignation()
        {
            AddAuthHeader();
            var model = new MasterViewModel();

            var response = await _client.GetAsync($"{_apiBaseUrl}/designations");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                model.Designations = JsonSerializer.Deserialize<List<DesignationViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DesignationViewModel>();
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDesignation(MasterViewModel model)
        {
            AddAuthHeader();
            var data = new { Name = model.DesigName, Level = model.DesigLevel ?? 1 };
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiBaseUrl}/designations", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Designation Created Successfully";
            else
                TempData["Error"] = "Failed to create Designation";

            return RedirectToAction("CreateDesignation");
        }

        // --- 3. POST PAGE (Create + List) ---
        [HttpGet]
        public async Task<IActionResult> CreatePost()
        {
            AddAuthHeader();
            var model = new MasterViewModel(); // Use MasterViewModel to hold List<Post>

            var response = await _client.GetAsync($"{_apiBaseUrl}/posts");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                model.Posts = JsonSerializer.Deserialize<List<Post>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Post>();
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(string PostName)
        {
            AddAuthHeader();
            var data = new { Name = PostName };
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiBaseUrl}/posts", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Post Created Successfully";
            else
                TempData["Error"] = "Failed to create Post";

            return RedirectToAction("CreatePost");
        }
        
        // --- Keep Index if you want a master summary, or remove it ---
        public IActionResult Index() => RedirectToAction("CreateDepartment"); 
    }
}