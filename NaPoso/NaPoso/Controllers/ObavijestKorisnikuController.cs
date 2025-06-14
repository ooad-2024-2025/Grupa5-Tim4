using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ObavijestKorisnikuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ObavijestKorisnikuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ObavijestKorisniku
        public async Task<IActionResult> Index()
        {
            return View(await _context.ObavijestKorisniku.ToListAsync());
        }

        // GET: ObavijestKorisniku/MyNotifications
        [Authorize]
        public async Task<IActionResult> MyNotifications()
        {
            // Get the current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Query directly from Obavijest table to match the navbar query
            var notifications = await _context.Obavijest
                .Where(x => x.KorisnikId == userId)
                .OrderByDescending(x => x.VrijemeSlanja)
                .ToListAsync();

            // REMOVED: Auto-marking as read when viewed
            // Now notifications stay unread until explicitly marked

            return View(notifications);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notification = await _context.Obavijest
                .FirstOrDefaultAsync(o => o.Id == id && o.KorisnikId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyNotifications));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var unreadNotifications = await _context.Obavijest
                .Where(o => o.KorisnikId == userId && (o.IsRead == null || o.IsRead == false))
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyNotifications));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearNotification(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notification = await _context.Obavijest
                .FirstOrDefaultAsync(o => o.Id == id && o.KorisnikId == userId);

            if (notification != null)
            {
                _context.Obavijest.Remove(notification);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyNotifications));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notifications = await _context.Obavijest
                .Where(o => o.KorisnikId == userId)
                .ToListAsync();

            _context.Obavijest.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyNotifications));
        }

        // GET: ObavijestKorisniku/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var obavijestKorisniku = await _context.ObavijestKorisniku
                .Include(ok => ok.obavijest)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (obavijestKorisniku == null)
            {
                return NotFound();
            }

            return View(obavijestKorisniku);
        }

        // GET: ObavijestKorisniku/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ObavijestKorisniku/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,KorisnikId")] ObavijestKorisniku obavijestKorisniku)
        {
            if (ModelState.IsValid)
            {
                _context.Add(obavijestKorisniku);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(obavijestKorisniku);
        }

        // GET: ObavijestKorisniku/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var obavijestKorisniku = await _context.ObavijestKorisniku.FindAsync(id);
            if (obavijestKorisniku == null)
            {
                return NotFound();
            }
            return View(obavijestKorisniku);
        }

        // POST: ObavijestKorisniku/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,KorisnikId")] ObavijestKorisniku obavijestKorisniku)
        {
            if (id != obavijestKorisniku.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(obavijestKorisniku);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ObavijestKorisnikuExists(obavijestKorisniku.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(obavijestKorisniku);
        }

        // GET: ObavijestKorisniku/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var obavijestKorisniku = await _context.ObavijestKorisniku
                .FirstOrDefaultAsync(m => m.Id == id);
            if (obavijestKorisniku == null)
            {
                return NotFound();
            }

            return View(obavijestKorisniku);
        }

        // POST: ObavijestKorisniku/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var obavijestKorisniku = await _context.ObavijestKorisniku.FindAsync(id);
            if (obavijestKorisniku != null)
            {
                _context.ObavijestKorisniku.Remove(obavijestKorisniku);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ObavijestKorisnikuExists(int id)
        {
            return _context.ObavijestKorisniku.Any(e => e.Id == id);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReadAjax(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notification = await _context.Obavijest
                .FirstOrDefaultAsync(o => o.Id == id && o.KorisnikId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Ok();
            }

            return NotFound();
        }
    }
}