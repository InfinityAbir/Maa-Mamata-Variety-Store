using Ecommerce.Models;
using Ecommerce.Helpers; // For session extension
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

        // Checkout page
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession") ?? new List<CartItem>();
            if (!cart.Any())
                return RedirectToAction("Index", "Products");

            return View(cart);
        }

        // Place order
        [HttpPost]
        public IActionResult PlaceOrder(string name, string email, string address, string phone)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession");
            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Products");

            var order = new Order
            {
                CustomerName = name,
                CustomerEmail = email,
                CustomerAddress = address,
                CustomerPhone = phone,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                PaymentMethod = "Cash on Delivery",
                Status = "Pending",
                OrderDate = DateTime.Now
            };
            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity
                };
                _context.OrderItems.Add(orderItem);

                var product = _context.Products.Find(item.ProductId);
                if (product != null)
                    product.Quantity -= item.Quantity;
            }

            _context.SaveChanges();

            if (!HttpContext.Session.Keys.Contains("UserEmail"))
                HttpContext.Session.SetString("CustomerEmail", email);

            HttpContext.Session.Remove("CartSession");

            return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
        }

        // Order confirmation page
        public IActionResult OrderConfirmation(int id)
        {
            var order = _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // Manage all orders (Admin)
        public IActionResult Manage()
        {
            var orders = _context.Orders
                                 .Include(o => o.OrderItems)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View(orders);
        }

        // Print single order (Admin/Customer)
        public IActionResult Print(int id)
        {
            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            // Shop info
            var shopInfo = new
            {
                Name = "My Shop Name",
                Address = "123 Main Street, City, Country",
                Phone = "0123456789",
                Email = "shop@example.com"
            };
            ViewBag.ShopInfo = shopInfo;

            return View(order);
        }

        // My Orders page
        public IActionResult MyOrders()
        {
            string email = HttpContext.Session.GetString("UserEmail") ??
                           HttpContext.Session.GetString("CustomerEmail");

            if (string.IsNullOrEmpty(email))
            {
                TempData["Message"] = "No orders found. Please place an order first.";
                return RedirectToAction("Index", "Products");
            }

            var orders = _context.Orders
                                 .Include(o => o.OrderItems)
                                 .Where(o => o.CustomerEmail == email)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View(orders);
        }

        // Edit order (Admin)
        public IActionResult Edit(int id)
        {
            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Order updatedOrder)
        {
            if (id != updatedOrder.OrderId)
                return BadRequest();

            var order = _context.Orders.Find(id);
            if (order == null)
                return NotFound();

            order.Status = updatedOrder.Status;
            order.PaymentMethod = updatedOrder.PaymentMethod;

            _context.SaveChanges();
            return RedirectToAction(nameof(Manage));
        }

        // Delete order (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null)
                return NotFound();

            _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            _context.SaveChanges();

            return RedirectToAction(nameof(Manage));
        }
    }
}
