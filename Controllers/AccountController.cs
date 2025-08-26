using System.Linq;
using Ecommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Controllers
{
    public class AccountController : Controller
    {
        private readonly EcommerceDbContext _context;

        public AccountController(EcommerceDbContext context)
        {
            _context = context;
        }

        // GET: Login
        [HttpGet]
        public ActionResult Login()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var role = HttpContext.Session.GetString("UserRole");
                return role switch
                {
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    "Customer" => RedirectToAction("Index", "Home"),
                    _ => RedirectToAction("Logout"), // Sellers or unknown roles
                };
            }

            return View(new LoginViewModel());
        }

        // POST: Login
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please fill all fields correctly.";
                return View(model);
            }

            var email = model.Email?.Trim().ToLower();
            var password = model.Password;

            var user = _context.Users
                .FirstOrDefault(u => u.Email.ToLower() == email
                                  && u.Password == password
                                  && u.Role.ToString() == model.Role.ToString());

            if (user != null)
            {
                // 🚫 Block Seller login
                if (user.Role == Role.Seller)
                {
                    ViewBag.Error = "Sellers are not allowed to log in.";
                    return View(model);
                }

                // Store session values
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role.ToString());
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);

                // Redirect based on role
                if (user.Role == Role.Admin)
                    return RedirectToAction("Index", "Home");

                return RedirectToAction("Index", "Home"); // Customer
            }

            ViewBag.Error = "Invalid credentials or role mismatch";
            return View(model);
        }

        // GET: Register
        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please correct the errors below.";
                return View(model);
            }

            // Check if email already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());
            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered.";
                return View(model);
            }

            // Enforce Customer role (no one can register as Admin or Seller directly)
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password, // ⚠️ Consider hashing
                Role = Role.Customer
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Store session values
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role.ToString());
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return RedirectToAction("Index", "Home");
        }

        // GET: Logout
        [HttpGet]
        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ✅ Hidden Admin Creation Endpoint
        [HttpGet]
        public ActionResult CreateAdmin()
        {
            // Check if admin already exists
            if (!_context.Users.Any(u => u.Role == Role.Admin))
            {
                var admin = new User
                {
                    Name = "Super Admin",
                    Email = "admin@yourapp.com",
                    Password = "123456", // ⚠️ You should hash this
                    Role = Role.Admin
                };

                _context.Users.Add(admin);
                _context.SaveChanges();

                return Content("✅ Admin account created. Email: admin@yourapp.com, Password: 123456");
            }

            return Content("⚠️ Admin already exists.");
        }
    }
}
