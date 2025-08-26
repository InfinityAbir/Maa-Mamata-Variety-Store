using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Helpers;
using Ecommerce.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers
{
    public class ProductsController : Controller
    {
        private readonly EcommerceDbContext _context;

        public ProductsController(EcommerceDbContext context)
        {
            _context = context;
        }

        // ===============================
        // HELPERS
        // ===============================
        private async Task<User?> GetCurrentUserAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task<bool> IsAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            return user != null && user.Role == Role.Admin;
        }

        private bool ProductExists(int id) => _context.Products.Any(e => e.Id == id);

        private async Task<string?> SaveImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
            Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return "/images/products/" + fileName;
        }

        private void DeleteImage(string? imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }

        // ===============================
        // INDEX (All Products)
        // ===============================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession") ?? new List<CartItem>();
            ViewBag.CartCount = cart.Sum(c => c.Quantity);
            return View(products);
        }

        // ===============================
        // SEARCH Products
        // ===============================
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return RedirectToAction(nameof(Index));

            var results = await _context.Products
                .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
                .ToListAsync();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession") ?? new List<CartItem>();
            ViewBag.CartCount = cart.Sum(c => c.Quantity);

            ViewBag.SearchQuery = query;
            return View("Index", results);
        }

        // ===============================
        // PRODUCT DETAILS
        // ===============================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // ===============================
        // CREATE PRODUCT (Admin)
        // ===============================
        public async Task<IActionResult> Create()
        {
            if (!await IsAdminAsync()) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,Quantity")] Product product, IFormFile? ImageFile)
        {
            if (!await IsAdminAsync()) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                product.ImagePath = await SaveImageAsync(ImageFile);
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage));
            }
            return View(product);
        }

        // ===============================
        // EDIT PRODUCT (Admin)
        // ===============================
        public async Task<IActionResult> Edit(int? id)
        {
            if (!await IsAdminAsync()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Quantity,ImagePath")] Product product, IFormFile? ImageFile)
        {
            if (!await IsAdminAsync()) return RedirectToAction("Index", "Home");
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (existingProduct == null) return NotFound();

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        DeleteImage(existingProduct.ImagePath);
                        product.ImagePath = await SaveImageAsync(ImageFile);
                    }
                    else
                    {
                        product.ImagePath = existingProduct.ImagePath;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Manage));
            }

            return View(product);
        }

        // ===============================
        // DELETE PRODUCT (Admin)
        // ===============================
        public async Task<IActionResult> Delete(int? id)
        {
            if (!await IsAdminAsync()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await IsAdminAsync()) return RedirectToAction("Index", "Home");

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                DeleteImage(product.ImagePath);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Manage));
        }

        // ===============================
        // MANAGE (Admin Panel)
        // ===============================
        public async Task<IActionResult> Manage()
        {
            if (!await IsAdminAsync()) return RedirectToAction("Index", "Home");
            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View(products);
        }

        // ===============================
        // BUY PRODUCT / ADD TO CART
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Buy(int productId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Error"] = "You must log in to buy products.";
                TempData["ErrorProductId"] = productId;
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                TempData["ErrorProductId"] = productId;
                return RedirectToAction(nameof(Index));
            }

            // Stock validation
            if (product.Quantity <= 0)
            {
                TempData["Error"] = "❌ Not available right now.";
                TempData["ErrorProductId"] = productId;
                return RedirectToAction(nameof(Index));
            }

            if (quantity > product.Quantity)
            {
                TempData["Error"] = $"❌ Only {product.Quantity} item(s) available.";
                TempData["ErrorProductId"] = productId;
                return RedirectToAction(nameof(Index));
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("CartSession") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (cartItem != null)
            {
                if (cartItem.Quantity + quantity > product.Quantity)
                {
                    TempData["Error"] = $"❌ You can only add {product.Quantity - cartItem.Quantity} more.";
                    TempData["ErrorProductId"] = productId;
                    return RedirectToAction(nameof(Index));
                }

                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            HttpContext.Session.SetObjectAsJson("CartSession", cart);
            TempData["Success"] = $"✅ {quantity} × {product.Name} added to cart!";
            TempData["SuccessProductId"] = productId;
            return RedirectToAction(nameof(Index));
        }



    }
}
