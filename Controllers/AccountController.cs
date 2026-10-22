using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using FullVoucherSystem.Data;
using FullVoucherSystem.Models;

namespace FullVoucherSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Login() => View();

        public IActionResult Signup() => View();

        [HttpPost]
        public IActionResult Signup(User user)
        {
            var exists = _context.Users.Any(x => x.Username == user.Username);

            if (exists)
            {
                ViewBag.Error = "Username already exists";
                return View();
            }

            _context.Users.Add(user);
            _context.SaveChanges();

            HttpContext.Session.SetString("Surname", user.Surname);
            HttpContext.Session.SetString("FirstName", user.FirstName);
            HttpContext.Session.SetString("Designation", user.Designation);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        public IActionResult Login(string username, string password, string role)
        {
            // Search Users table
            var user = _context.Users.FirstOrDefault(x =>
                        x.Username == username &&
                        x.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Incorrect Username or Password";
                return View();
            }

            // Validate Role
            if (user.Role != role)
            {
                ViewBag.Error = "Incorrect Role Selected";
                return View();
            }

            // Save user information in Session
            HttpContext.Session.SetString("Surname", user.Surname);
            HttpContext.Session.SetString("FirstName", user.FirstName);
            HttpContext.Session.SetString("Designation", user.Designation);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("Index", "Dashboard");
        }

        // --- NEW: LOGOUT ACTION ---
        public IActionResult Logout()
        {
            // Clear all session values
            HttpContext.Session.Clear();

            // Redirect back to the Login page
            return RedirectToAction("Login", "Account");
        }
    }
}