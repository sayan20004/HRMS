using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRMS.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBaseUrl = "http://localhost:5173/api";

        public EmployeeController()
        {
            _client = new HttpClient();
        }

        private void AddAuthHeader()
{
    // 1. Try Session first
    var token = HttpContext.Session.GetString("Token");

    // 2. If Session is empty, try to get from User Claims (Cookie)
    if (string.IsNullOrEmpty(token) && User.Identity!.IsAuthenticated)
    {
        token = User.FindFirst("Token")?.Value;
        
        // Restore Session for subsequent requests to save parsing time
        if (!string.IsNullOrEmpty(token))
        {
             HttpContext.Session.SetString("Token", token);
             HttpContext.Session.SetString("Username", User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "");
             HttpContext.Session.SetString("Email", User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "");
        }
    }

    if (!string.IsNullOrEmpty(token))
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

        // --- INDEX WITH SEARCH ---
        public async Task<IActionResult> Index(string searchString)
        {
            AddAuthHeader();
            await LoadDropdowns(); 

            var response = await _client.GetAsync($"{_apiBaseUrl}/employee");
            List<EmployeeViewModel> employees = new List<EmployeeViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                employees = JsonSerializer.Deserialize<List<EmployeeViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EmployeeViewModel>();
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                employees = employees.Where(e => 
                    e.FullName.ToLower().Contains(searchString) || 
                    e.Email.ToLower().Contains(searchString) ||
                    e.MobileNumber.Contains(searchString)
                ).ToList();
            }

            ViewData["CurrentFilter"] = searchString;
            return View(employees);
        }

        // --- GET SINGLE EMPLOYEE (AJAX) ---
        [HttpGet]
        public async Task<IActionResult> GetEmployee(int id)
        {
            AddAuthHeader();
            var response = await _client.GetAsync($"{_apiBaseUrl}/employee/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            return NotFound();
        }

        // --- CREATE ---
        [HttpPost]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            AddAuthHeader();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_apiBaseUrl}/employee", content);

            if (response.IsSuccessStatusCode) TempData["Success"] = "Employee created successfully!";
            else TempData["Error"] = "Failed to create employee.";

            return RedirectToAction("Index");
        }

        // --- EDIT ---
        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
            AddAuthHeader();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_apiBaseUrl}/employee/{model.Id}", content);

            if (response.IsSuccessStatusCode) TempData["Success"] = "Employee updated successfully!";
            else TempData["Error"] = "Failed to update employee.";

            return RedirectToAction("Index");
        }

        // --- DELETE ---
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync($"{_apiBaseUrl}/employee/{id}");
            
            if (response.IsSuccessStatusCode) TempData["Success"] = "Employee deleted successfully!";
            else TempData["Error"] = "Failed to delete employee.";

            return RedirectToAction("Index");
        }

        private async Task LoadDropdowns()
        {
            AddAuthHeader();
            
            // Departments
            var deptRes = await _client.GetAsync($"{_apiBaseUrl}/master/departments");
            if (deptRes.IsSuccessStatusCode)
            {
                var content = await deptRes.Content.ReadAsStringAsync();
                var depts = JsonSerializer.Deserialize<List<DepartmentViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ViewBag.Departments = new SelectList(depts, "Id", "Name");
            }

            // Designations
            var desigRes = await _client.GetAsync($"{_apiBaseUrl}/master/designations");
            if (desigRes.IsSuccessStatusCode)
            {
                var content = await desigRes.Content.ReadAsStringAsync();
                var desigs = JsonSerializer.Deserialize<List<DesignationViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ViewBag.Designations = new SelectList(desigs, "Id", "Name");
            }

            // --- FETCH POSTS ---
            var postRes = await _client.GetAsync($"{_apiBaseUrl}/master/posts");
            if (postRes.IsSuccessStatusCode)
            {
                var content = await postRes.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<Post>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ViewBag.Posts = new SelectList(posts, "Id", "Name");
            }
        }
    }
}