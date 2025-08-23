using Microsoft.AspNetCore.Mvc;
using Ecommerce.Models;
using Ecommerce.Helpers; // For session extension
using System.Collections.Generic;
using System.Linq;

namespace Ecommerce.Controllers
{
    public class CartController : Controller
    {
        private readonly EcommerceDbContext _context;
        private const string CartSessionKey = "CartSession";

        public CartController(EcommerceDbContext context)
        {
            _context = context;
        }

        // Show Cart
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            return View(cart);
        }

        // Add product to cart (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);

            if (cartItem != null)
            {
                if (cartItem.Quantity + quantity > product.Quantity)
                {
                    TempData["Error"] = $"Only {product.Quantity} items available in stock.";
                    return RedirectToAction("Index", "Products");
                }
                cartItem.Quantity += quantity;
            }
            else
            {
                if (quantity > product.Quantity)
                {
                    TempData["Error"] = $"Only {product.Quantity} items available in stock.";
                    return RedirectToAction("Index", "Products");
                }
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            TempData["Success"] = $"{product.Name} added to cart!";
            return RedirectToAction("Index", "Cart");
        }

        // Increase quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IncreaseQuantity(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == id);
                if (product != null && item.Quantity < product.Quantity)
                {
                    item.Quantity++;
                }
                else
                {
                    TempData["Error"] = $"Cannot add more than {product.Quantity} items for {product.Name}.";
                }
            }
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            return RedirectToAction("Index");
        }

        // Decrease quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DecreaseQuantity(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    cart.Remove(item); // Remove item if quantity reaches 0
                }
            }
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            return RedirectToAction("Index");
        }

        // Update quantity in cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == id);
                if (product == null) return NotFound();

                if (quantity <= 0)
                    cart.Remove(item);
                else if (quantity <= product.Quantity)
                    item.Quantity = quantity;
                else
                    TempData["Error"] = $"Only {product.Quantity} items available in stock.";
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            return RedirectToAction("Index");
        }

        // Remove item completely
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null) cart.Remove(item);

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            return RedirectToAction("Index");
        }

        // Clear entire cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction("Index");
        }
    }
}
