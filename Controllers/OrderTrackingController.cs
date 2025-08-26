using Ecommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers
{
    public class OrderTrackingController : Controller
    {
        private readonly EcommerceDbContext _context;
        public OrderTrackingController(EcommerceDbContext context)
        {
            _context = context;
        }

        // Helper method to check if user is logged in
        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")) ||
                   !string.IsNullOrEmpty(HttpContext.Session.GetString("CustomerEmail"));
        }

        // Show search form
        [HttpGet]
        public IActionResult Track()
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // Handle search
        [HttpPost]
        public IActionResult Track(string trackingNumber)
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(trackingNumber))
            {
                ViewBag.Message = "Please enter a tracking number.";
                return View();
            }

            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product) // ✅ Load Product for each item
                                .FirstOrDefault(o => o.TrackingNumber == trackingNumber);

            if (order == null)
            {
                ViewBag.Message = "No order found with this tracking number.";
                return View();
            }

            return View("TrackResult", order);
        }
    }
}
