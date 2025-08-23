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
            // If already logged in, redirect based on role
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var role = HttpContext.Session.GetString("UserRole");
                return role switch
                {
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    "Seller" => RedirectToAction("Dashboard", "Seller"),
                    _ => RedirectToAction("Index", "Home"),
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
                // Store session values
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role.ToString());
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserEmail", user.Email); // <-- Add email to session

                // Redirect based on role
                return RedirectToAction("Index", "Home");
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

            // Enforce Customer role
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password, // Consider hashing
                Role = Role.Customer
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Store session values
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role.ToString());
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email); // <-- Add email to session

            return RedirectToAction("Index", "Home");
        }

        // GET: Logout
        [HttpGet]
        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
