using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Check if user is logged in
            var username = HttpContext.Session.GetString("Username");
            
            if (string.IsNullOrEmpty(username))
            {
                // Redirect to register  if not logged in
                return RedirectToAction("Index", "Register");
            }
            
            // Pass username to view
            ViewData["Username"] = username;
            ViewData["Title"] = "Dashboard";
            
            return View();
        }

        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();
            
            // Clear Remember Me cookies
            Response.Cookies.Delete("RememberMe_Email");
            Response.Cookies.Delete("RememberMe_Username");
            
            // Redirect to home 
            return RedirectToAction("Index", "Home");
        }
    }
}
