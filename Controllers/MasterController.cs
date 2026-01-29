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

            // 1. Fetch Departments
            var deptRes = await _client.GetAsync($"{_apiBaseUrl}/departments");
            if (deptRes.IsSuccessStatusCode)
            {
                var content = await deptRes.Content.ReadAsStringAsync();
                model.Departments = JsonSerializer.Deserialize<List<DepartmentViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DepartmentViewModel>();
            }

            // 2. Fetch Designations
            var desigRes = await _client.GetAsync($"{_apiBaseUrl}/designations");
            if (desigRes.IsSuccessStatusCode)
            {
                var content = await desigRes.Content.ReadAsStringAsync();
                model.Designations = JsonSerializer.Deserialize<List<DesignationViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DesignationViewModel>();
            }

            return View(model);
        }

        [HttpPost]
public async Task<IActionResult> Create(MasterViewModel model)
{
    AddAuthHeader();
    string errorMessage = "";

    // Save Department
    if (!string.IsNullOrEmpty(model.DeptName))
    {
        var deptData = new { Name = model.DeptName, Code = model.DeptCode };
        var content = new StringContent(JsonSerializer.Serialize(deptData), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{_apiBaseUrl}/departments", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var apiError = await response.Content.ReadAsStringAsync();
            errorMessage += $"Department Error: {apiError} ";
        }
    }

    // Save Designation
    if (!string.IsNullOrEmpty(model.DesigName))
    {
        var desigData = new { Name = model.DesigName, Level = model.DesigLevel ?? 1 };
        var content = new StringContent(JsonSerializer.Serialize(desigData), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{_apiBaseUrl}/designations", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var apiError = await response.Content.ReadAsStringAsync();
            errorMessage += $"Designation Error: {apiError} ";
        }
    }

    // Feedback
    if (!string.IsNullOrEmpty(errorMessage))
        TempData["Error"] = errorMessage; //  error on screen
    else
        TempData["Success"] = "Master data saved successfully!";

    return RedirectToAction("Index");
}
    }
}