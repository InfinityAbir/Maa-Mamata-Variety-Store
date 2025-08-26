using Ecommerce.Helpers; // For session extension
using Ecommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers
{
    public class OrderController : Controller
    {
        private readonly EcommerceDbContext _context;

        public OrderController(EcommerceDbContext context)
        {
            _context = context;
        }

        // Admin check
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role != null && role == "Admin";
        }

        private bool IsUserLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null || HttpContext.Session.Keys.Contains("CustomerEmail");
        }

        // ===============================
        // Customer Checkout & Place Order
        // ===============================
        public IActionResult Checkout()
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession") ?? new List<CartItem>();
            if (!cart.Any())
                return RedirectToAction("Index", "Products");

            ViewBag.Cart = cart;
            return View(cart);
        }

        [HttpPost]
        public IActionResult PlaceOrder(string name, string email, string address, string phone, string deliveryLocation)
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession");
            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Products");

            decimal subtotal = cart.Sum(c => c.Price * c.Quantity);
            decimal deliveryCharge = deliveryLocation == "Inside Dhaka" ? 60 : 100;
            decimal totalAmount = subtotal + deliveryCharge;

            var order = new Order
            {
                CustomerName = name,
                CustomerEmail = email,
                CustomerAddress = address,
                CustomerPhone = phone,
                DeliveryCharge = deliveryCharge,
                TotalAmount = totalAmount,
                PaymentMethod = "Cash on Delivery",
                Status = "Pending",
                OrderDate = DateTime.Now,
                TrackingNumber = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity
                });

                var product = _context.Products.Find(item.ProductId);
                if (product != null)
                    product.Quantity -= item.Quantity;
            }

            _context.SaveChanges();

            if (!HttpContext.Session.Keys.Contains("UserEmail"))
                HttpContext.Session.SetString("CustomerEmail", email);

            HttpContext.Session.Remove("CartSession");

            TempData["Success"] = "✅ Your order has been placed successfully!";
            return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
        }

        public IActionResult OrderConfirmation(int id)
        {
            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            return View(order);
        }

        public IActionResult MyOrders()
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            string email = HttpContext.Session.GetString("UserEmail") ?? HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "No orders found. Please place an order first.";
                return RedirectToAction("Index", "Products");
            }

            var orders = _context.Orders
                                 .Include(o => o.OrderItems)
                                 .Where(o => o.CustomerEmail == email)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            if (order.Status == "Pending" || order.Status == "Processing")
            {
                order.Status = "Cancelled";
                _context.SaveChanges();
            }

            return RedirectToAction("MyOrders");
        }

        // ===============================
        // Admin: Manage Orders
        // ===============================
        public IActionResult Manage()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                                 .Include(o => o.OrderItems)
                                 .ThenInclude(oi => oi.Product)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View(orders);
        }

        // ===============================
        // Admin: Edit Order
        // ===============================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Order model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id != model.OrderId) return BadRequest();

            if (ModelState.IsValid)
            {
                var order = _context.Orders.Find(id);
                if (order == null) return NotFound();

                // Update order details
                order.Status = model.Status;
                order.CustomerName = model.CustomerName;
                order.CustomerEmail = model.CustomerEmail;
                order.CustomerPhone = model.CustomerPhone;
                order.CustomerAddress = model.CustomerAddress;

                _context.SaveChanges();
                TempData["Success"] = "✅ Order updated successfully!";
                return RedirectToAction("Manage");
            }

            return View(model);
        }

        // ===============================
        // Admin: Delete Order
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            _context.SaveChanges();

            TempData["Success"] = "✅ Order deleted successfully!";
            return RedirectToAction("Manage");
        }

        // ===============================
        // Admin: Print Order
        // ===============================
        public IActionResult Print(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            return View(order);
        }
    }
}
