using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Models;

namespace Ecommerce.Controllers
{
    public class UsersController : Controller
    {
        private readonly EcommerceDbContext _context;

        public UsersController(EcommerceDbContext context)
        {
            _context = context;
        }

        private async Task<User> GetCurrentUserAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task<bool> IsAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            return user != null && user.Role == Role.Admin;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(user);
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.Role != Role.Admin)
                return RedirectToAction("Login", "Account");

            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.Role != Role.Admin)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var targetUser = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (targetUser == null)
                return NotFound();

            return View(targetUser);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Password,Role")] User user)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Password,Role")] User user)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Account");

            if (id != user.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
                return NotFound();

            ViewBag.AdminCount = await _context.Users.CountAsync(u => u.Role == Role.Admin);

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var adminCount = await _context.Users.CountAsync(u => u.Role == Role.Admin);

            if (user.Role == Role.Admin && adminCount <= 1)
            {
                TempData["Error"] = "Cannot delete the only admin user.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
