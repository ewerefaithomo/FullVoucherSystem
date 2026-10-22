
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FullVoucherSystem.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var surname = HttpContext.Session.GetString("Surname");
            var firstName = HttpContext.Session.GetString("FirstName");

            // Validate required session variables.
            if (string.IsNullOrWhiteSpace(role) ||
                string.IsNullOrWhiteSpace(surname) ||
                string.IsNullOrWhiteSpace(firstName))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Role = role;
            ViewBag.Surname = surname;
            ViewBag.FirstName = firstName;

            return View();
        }
    }
}
