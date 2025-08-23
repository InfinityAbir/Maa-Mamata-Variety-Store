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
            return View("Index", results); // reuse Index view
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
        // CREATE PRODUCT
        // ===============================
        public IActionResult Create()
        {
            if (!IsAdmin()) return Forbid();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,Quantity")] Product product, IFormFile? ImageFile)
        {
            if (!IsAdmin()) return Forbid();

            if (ModelState.IsValid)
            {
                product.ImagePath = await SaveImageAsync(ImageFile);
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // ===============================
        // EDIT PRODUCT
        // ===============================
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Quantity,ImagePath")] Product product, IFormFile? ImageFile)
        {
            if (!IsAdmin()) return Forbid();
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (existingProduct == null) return NotFound();

                    // Handle image upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Delete old image
                        DeleteImage(existingProduct.ImagePath);
                        product.ImagePath = await SaveImageAsync(ImageFile);
                    }
                    else
                    {
                        // Keep old image
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
        // DELETE PRODUCT
        // ===============================
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Forbid();

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
        public IActionResult Manage()
        {
            if (!IsAdmin()) return Forbid();
            var products = _context.Products.OrderBy(p => p.Name).ToList();
            return View(products);
        }

        // ===============================
        // HELPERS
        // ===============================
        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin";
        }

        private bool ProductExists(int id) =>
            _context.Products.Any(e => e.Id == id);

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
    }
}
