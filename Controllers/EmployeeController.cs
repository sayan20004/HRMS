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

        // --- 1. INDEX ( Loads Dropdowns for Modals) ---
        public async Task<IActionResult> Index(string searchString)
        {
            AddAuthHeader();
            await LoadDropdowns(); // Load data for the Modals

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
                    e.Email.ToLower().Contains(searchString)
                ).ToList();
            }

            return View(employees);
        }

        // --- 2. GET SINGLE EMPLOYEE (For Edit Modal AJAX) ---
        [HttpGet]
        public async Task<IActionResult> GetEmployee(int id)
        {
            AddAuthHeader();
            var response = await _client.GetAsync($"{_apiBaseUrl}/employee/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // Return JSON directly to the modal
                return Content(content, "application/json");
            }
            return NotFound();
        }

        // --- 3. CREATE (POST Only) ---
        [HttpPost]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            AddAuthHeader();
            // Note: If model state is invalid, we redirect to Index with error (simplest for Modals without complex AJAX form handling)
            
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_apiBaseUrl}/employee", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Employee created successfully!";
            else
                TempData["Error"] = "Failed to create employee.";

            return RedirectToAction("Index");
        }

        // --- 4. EDIT (POST Only) ---
        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
            AddAuthHeader();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_apiBaseUrl}/employee/{model.Id}", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Employee updated successfully!";
            else
                TempData["Error"] = "Failed to update employee.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync($"{_apiBaseUrl}/employee/{id}");
            if (response.IsSuccessStatusCode) TempData["Success"] = "Employee deleted.";
            else TempData["Error"] = "Delete failed.";
            return RedirectToAction("Index");
        }

        private async Task LoadDropdowns()
        {
            AddAuthHeader();
            // Fetch Departments
            var deptRes = await _client.GetAsync($"{_apiBaseUrl}/master/departments");
            if (deptRes.IsSuccessStatusCode)
            {
                var content = await deptRes.Content.ReadAsStringAsync();
                var depts = JsonSerializer.Deserialize<List<DepartmentViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ViewBag.Departments = new SelectList(depts, "Id", "Name");
            }
            // Fetch Designations
            var desigRes = await _client.GetAsync($"{_apiBaseUrl}/master/designations");
            if (desigRes.IsSuccessStatusCode)
            {
                var content = await desigRes.Content.ReadAsStringAsync();
                var desigs = JsonSerializer.Deserialize<List<DesignationViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ViewBag.Designations = new SelectList(desigs, "Id", "Name");
            }
        }
    }
}