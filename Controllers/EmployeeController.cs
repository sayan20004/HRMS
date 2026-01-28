using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using System.Text.Json;
using System.Text;

namespace HRMS.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBaseUrl = "http://localhost:5173/api/employee";

        public EmployeeController()
        {
            _client = new HttpClient();
        }

        private bool IsSessionExpired()
        {
            return string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        }

        public async Task<IActionResult> Index()
        {
            if (IsSessionExpired()) return RedirectToAction("Login", "Register");

            var employees = new List<EmployeeViewModel>();
            
            try 
            {
                var response = await _client.GetAsync(_apiBaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    employees = JsonSerializer.Deserialize<List<EmployeeViewModel>>(content, options) ?? new List<EmployeeViewModel>();
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Unable to connect to the Employee API.");
            }

            return View(employees);
        }

        public IActionResult Create()
        {
            if (IsSessionExpired()) return RedirectToAction("Login", "Register");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (IsSessionExpired()) return RedirectToAction("Login", "Register");

            if (ModelState.IsValid)
            {
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(_apiBaseUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (IsSessionExpired()) return RedirectToAction("Login", "Register");

            var response = await _client.GetAsync($"{_apiBaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var employee = JsonSerializer.Deserialize<EmployeeViewModel>(content, options);
                return View(employee);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
        {
            if (IsSessionExpired()) return RedirectToAction("Login", "Register");
            if (id != model.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PutAsync($"{_apiBaseUrl}/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (IsSessionExpired()) return RedirectToAction("Login", "Register");

            await _client.DeleteAsync($"{_apiBaseUrl}/{id}");
            return RedirectToAction("Index");
        }
    }
}