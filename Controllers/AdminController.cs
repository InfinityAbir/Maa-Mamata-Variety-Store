using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    public class AdminController : Controller
    {
        private readonly EcommerceDbContext _context;

        public AdminController(EcommerceDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return View(user);
        }
    }
}
