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
            if (!string.IsNullOrEmpty(token))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AddAuthHeader();
            var model = new MasterViewModel();

            // Fetch Departments
            var deptRes = await _client.GetAsync($"{_apiBaseUrl}/departments");
            if (deptRes.IsSuccessStatusCode)
            {
                var content = await deptRes.Content.ReadAsStringAsync();
                model.Departments = JsonSerializer.Deserialize<List<DepartmentViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DepartmentViewModel>();
            }

            // Fetch Designations
            var desigRes = await _client.GetAsync($"{_apiBaseUrl}/designations");
            if (desigRes.IsSuccessStatusCode)
            {
                var content = await desigRes.Content.ReadAsStringAsync();
                model.Designations = JsonSerializer.Deserialize<List<DesignationViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DesignationViewModel>();
            }

            return View(model);
        }

        // --- CREATE (Handles both Dept and Desig) ---
        [HttpPost]
        public async Task<IActionResult> Create(MasterViewModel model)
        {
            AddAuthHeader();
            bool deptSuccess = true;
            bool desigSuccess = true;

            // Save Department
            if (!string.IsNullOrEmpty(model.DeptName))
            {
                var deptData = new { Name = model.DeptName, Code = model.DeptCode };
                var content = new StringContent(JsonSerializer.Serialize(deptData), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{_apiBaseUrl}/departments", content);
                if (!response.IsSuccessStatusCode) deptSuccess = false;
            }

            // Save Designation
            if (!string.IsNullOrEmpty(model.DesigName))
            {
                var desigData = new { Name = model.DesigName, Level = model.DesigLevel ?? 1 };
                var content = new StringContent(JsonSerializer.Serialize(desigData), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{_apiBaseUrl}/designations", content);
                if (!response.IsSuccessStatusCode) desigSuccess = false;
            }

            if (!deptSuccess || !desigSuccess)
                TempData["Error"] = "Some data could not be saved.";
            else
                TempData["Success"] = "Data saved successfully!";

            return RedirectToAction("Index");
        }

        // --- EDIT DEPARTMENT ---
        [HttpPost]
        public async Task<IActionResult> EditDepartment(int Id, string Name, string Code)
        {
            AddAuthHeader();
            var model = new { Id, Name, Code };
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_apiBaseUrl}/departments/{Id}", content);

            if (response.IsSuccessStatusCode) TempData["Success"] = "Department updated.";
            else TempData["Error"] = "Update failed.";

            return RedirectToAction("Index");
        }

        // --- EDIT DESIGNATION ---
        [HttpPost]
        public async Task<IActionResult> EditDesignation(int Id, string Name, int Level)
        {
            AddAuthHeader();
            var model = new { Id, Name, Level };
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_apiBaseUrl}/designations/{Id}", content);

            if (response.IsSuccessStatusCode) TempData["Success"] = "Designation updated.";
            else TempData["Error"] = "Update failed.";

            return RedirectToAction("Index");
        }

        // --- DELETE DEPARTMENT ---
        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync($"{_apiBaseUrl}/departments/{id}");
            if (response.IsSuccessStatusCode) TempData["Success"] = "Department deleted.";
            else TempData["Error"] = "Delete failed (It might be in use).";
            return RedirectToAction("Index");
        }

        // --- DELETE DESIGNATION ---
        [HttpPost]
        public async Task<IActionResult> DeleteDesignation(int id)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync($"{_apiBaseUrl}/designations/{id}");
            if (response.IsSuccessStatusCode) TempData["Success"] = "Designation deleted.";
            else TempData["Error"] = "Delete failed (It might be in use).";
            return RedirectToAction("Index");
        }
    }
}