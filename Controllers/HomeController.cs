using System.Diagnostics;
using Ecommerce.Helpers;
using Ecommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly EcommerceDbContext _context;

        public HomeController(ILogger<HomeController> logger, EcommerceDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            // Fetch all products to display on home page
            var products = await _context.Products.ToListAsync();

            // Load cart count from session (if exists)
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession") ?? new List<CartItem>();
            ViewBag.CartCount = cart.Sum(c => c.Quantity);

            return View(products); // You can reuse Products/Index view here
        }

        // GET: Privacy (optional)
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
