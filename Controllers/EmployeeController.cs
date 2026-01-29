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
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<IActionResult> Index()
        {
            AddAuthHeader();
            var response = await _client.GetAsync($"{_apiBaseUrl}/employee");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var employees = JsonSerializer.Deserialize<List<EmployeeViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(employees);
            }
            return View(new List<EmployeeViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            AddAuthHeader();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_apiBaseUrl}/employee", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Employee created successfully!";
                return RedirectToAction("Index");
            }
            
            // Handle API Errors (e.g., Duplicate Mobile Number)
            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Error: {errorContent}");
            
            await LoadDropdowns();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            AddAuthHeader();
            await LoadDropdowns();

            var response = await _client.GetAsync($"{_apiBaseUrl}/employee/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var employee = JsonSerializer.Deserialize<EmployeeViewModel>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(employee);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
            AddAuthHeader();
            
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_apiBaseUrl}/employee/{model.Id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Employee updated successfully!";
                return RedirectToAction("Index");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Update Failed: {errorContent}");
            
            await LoadDropdowns();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync($"{_apiBaseUrl}/employee/{id}");
            
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Employee deleted successfully!";
            else
                TempData["Error"] = "Failed to delete employee.";

            return RedirectToAction("Index");
        }

        // Helper to populate Dropdowns
        private async Task LoadDropdowns()
        {
            AddAuthHeader();

            // 1. Get Departments
            var deptResponse = await _client.GetAsync($"{_apiBaseUrl}/master/departments");
            if (deptResponse.IsSuccessStatusCode)
            {
                var content = await deptResponse.Content.ReadAsStringAsync();
                var depts = JsonSerializer.Deserialize<List<DepartmentViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ViewBag.Departments = new SelectList(depts, "Id", "Name");
            }

            // 2. Get Designations
            var desigResponse = await _client.GetAsync($"{_apiBaseUrl}/master/designations");
            if (desigResponse.IsSuccessStatusCode)
            {
                var content = await desigResponse.Content.ReadAsStringAsync();
                var desigs = JsonSerializer.Deserialize<List<DesignationViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ViewBag.Designations = new SelectList(desigs, "Id", "Name");
            }
        }
    }
}