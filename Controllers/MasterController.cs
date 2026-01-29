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

        // --- Method to Attach Token ---
        private void AddAuthHeader()
        {
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AddAuthHeader();
            var model = new MasterViewModel();

            try
            {
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
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to connect to API.";
            }

            return View(model);
        }
        [HttpPost]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        AddAuthHeader();
        var response = await _client.DeleteAsync($"{_apiBaseUrl}/departments/{id}");
        
        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Department deleted successfully.";
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Failed to delete: {error}";
        }
            
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDesignation(int id)
    {
        AddAuthHeader();
        var response = await _client.DeleteAsync($"{_apiBaseUrl}/designations/{id}");
        
        if (response.IsSuccessStatusCode)
                TempData["Success"] = "Designation deleted successfully.";
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Failed to delete: {error}";
        }

        return RedirectToAction("Index");
    }

        [HttpPost]
        public async Task<IActionResult> Create(MasterViewModel model)
        {
            AddAuthHeader(); 
            string errorMessage = "";

            // 1. Save Department (Only if name is provided)
            if (!string.IsNullOrEmpty(model.DeptName))
            {
                var deptData = new { Name = model.DeptName, Code = model.DeptCode };
                var content = new StringContent(JsonSerializer.Serialize(deptData), Encoding.UTF8, "application/json");
                
                try 
                {
                    var response = await _client.PostAsync($"{_apiBaseUrl}/departments", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        var apiError = await response.Content.ReadAsStringAsync();
                        errorMessage += $"Dept Error: {apiError} | ";
                    }
                }
                catch
                {
                    errorMessage += "Dept Error: API Unavailable | ";
                }
            }

            // 2. Save Designation (Only if name is provided)
            if (!string.IsNullOrEmpty(model.DesigName))
            {
                var desigData = new { Name = model.DesigName, Level = model.DesigLevel ?? 1 };
                var content = new StringContent(JsonSerializer.Serialize(desigData), Encoding.UTF8, "application/json");
                
                try
                {
                    var response = await _client.PostAsync($"{_apiBaseUrl}/designations", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        var apiError = await response.Content.ReadAsStringAsync();
                        errorMessage += $"Desig Error: {apiError} | ";
                    }
                }
                catch
                {
                    errorMessage += "Desig Error: API Unavailable | ";
                }
            }

            // 3. Set Feedback Message
            if (!string.IsNullOrEmpty(errorMessage))
                TempData["Error"] = errorMessage;
            else
                TempData["Success"] = "Saved successfully!";

            return RedirectToAction("Index");
        }
    }
}