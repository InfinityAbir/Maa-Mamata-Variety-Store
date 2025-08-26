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

        // List of areas inside Dhaka
        private readonly List<string> DhakaAreas = new List<string>
        {
            "Adabor", "Agargaon", "Aftab Nagar", "Ashulia", "Badda", "Banasree", "Banani", "Baridhara", "Basabo", "Bashundhara",
            "Baunia", "Birulia", "Boshundhara", "Cantonment", "Chandni Chowk", "Chowk Bazaar", "Dakshinkhan", "Dhanmondi",
            "Diabari", "Farmgate", "Gabtali", "Gulshan", "Hazaribagh", "Islampur", "Jatrabari", "Jinjira", "Kafrul", "Kallyanpur",
            "Khilgaon", "Khilkhet", "Kochukhet", "Lalbagh", "Lalmatia", "Manikdi", "Matikata", "Mohakhali", "Mohammadpur",
            "Monipur", "Motijheel", "Nimtoli", "Paltan", "Pallabi", "Rampura", "Sadarghat", "Savar",
            "Segunbagicha", "Shahbagh", "Shahjadpur", "Shyamoli", "Sutrapur", "Tejgaon", "Tejgaon Industrial Area", "Vatara",
            "Vashantek", "Wari", "Uttara", "Uttarkhan", "Gabtali", "Mirpur"
        };

        private bool IsUserLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null || HttpContext.Session.Keys.Contains("CustomerEmail");
        }

        private bool IsInsideDhaka(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return false;

            address = address.ToLower();
            return DhakaAreas.Any(area => address.Contains(area.ToLower()));
        }

        // ===============================
        // Checkout page
        // ===============================
        public IActionResult Checkout()
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession") ?? new List<CartItem>();
            if (!cart.Any())
                return RedirectToAction("Index", "Products");

            ViewBag.Cart = cart;
            ViewBag.DhakaAreas = DhakaAreas;

            return View(cart);
        }

        // ===============================
        // Place Order
        // ===============================
        [HttpPost]
        public IActionResult PlaceOrder(string name, string email, string address, string phone, string deliveryLocation)
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession");
            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Products");

            bool isAddressInsideDhaka = IsInsideDhaka(address);

            // Validate delivery location
            if ((deliveryLocation == "Inside Dhaka" && !isAddressInsideDhaka) ||
                (deliveryLocation == "Outside Dhaka" && isAddressInsideDhaka))
            {
                TempData["Error"] = "❌ Your selected delivery location does not match your address. Please correct it.";
                return RedirectToAction("Checkout");
            }

            // Stock validation
            foreach (var item in cart)
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    TempData["Error"] = $"❌ Product {item.ProductName} no longer exists.";
                    return RedirectToAction("Checkout");
                }
                if (product.Quantity < item.Quantity)
                {
                    TempData["Error"] = $"❌ Sorry, \"{product.Name}\" is not available in the requested quantity. Only {product.Quantity} left.";
                    return RedirectToAction("Checkout");
                }
            }

            decimal subtotal = cart.Sum(c => c.Price * c.Quantity);
            decimal deliveryCharge = isAddressInsideDhaka ? 60 : 100;
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

            // Add order items and reduce stock
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

            // Save guest email
            if (!HttpContext.Session.Keys.Contains("UserEmail"))
                HttpContext.Session.SetString("CustomerEmail", email);

            HttpContext.Session.Remove("CartSession");

            TempData["Success"] = "✅ Your order has been placed successfully!";
            return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
        }

        // ===============================
        // Order Confirmation
        // ===============================
        public IActionResult OrderConfirmation(int id)
        {
            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            return View(order);
        }

        // ===============================
        // My Orders (Customer)
        // ===============================
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

        // ===============================
        // Manage Orders (Admin)
        // ===============================
        public IActionResult Manage()
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                                 .Include(o => o.OrderItems)
                                 .ThenInclude(oi => oi.Product)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View(orders);
        }

        // ===============================
        // Cancel Order (Customer)
        // ===============================
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
        // Delete Order (Admin)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            _context.SaveChanges();

            return RedirectToAction("Manage");
        }
    }
}
