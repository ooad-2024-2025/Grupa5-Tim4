using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    public class ObavijestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager;
        public ObavijestController(ApplicationDbContext context, UserManager<Korisnik> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Obavijest
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var obavijesti = await _context.Obavijest
                .Where(o => o.KorisnikId == userId)
                .OrderByDescending(o => o.VrijemeSlanja)
                .ToListAsync();
            return View(obavijesti);
        }

        // GET: Obavijest/Details/5
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var obavijest = await _context.Obavijest
                .FirstOrDefaultAsync(m => m.Id == id);
            if (obavijest == null)
            {
                return NotFound();
            }

            return View(obavijest);
        }

        // GET: Obavijest/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Obavijest/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,KorisnikId,Sadrzaj,VrijemeSlanja,Tip")] Obavijest obavijest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(obavijest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(obavijest);
        }

        // GET: Obavijest/Edit/5
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Edit(int id)
        {
            var obavijest = await _context.Obavijest.FindAsync(id);
            if (obavijest == null)
                return NotFound();

            // Kreiraj SelectList od enum Obavjestenje
            ViewData["TipList"] = new SelectList(Enum.GetValues(typeof(Enums.Enums.Obavjestenje))
                .Cast<Enums.Enums.Obavjestenje>()
                .Select(t => new { Value = t, Text = t.ToString() }), "Value", "Text", obavijest.Tip);

            return View(obavijest);
        }

        // POST: Obavijest/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,KorisnikId,Sadrzaj,VrijemeSlanja,Tip")] Obavijest obavijest)
        {
            if (id != obavijest.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(obavijest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ObavijestExistsAsync(obavijest.Id))
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
            return View(obavijest);
        }

        // GET: Obavijest/Delete/5
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var obavijest = await _context.Obavijest
                .FirstOrDefaultAsync(m => m.Id == id);
            if (obavijest == null)
            {
                return NotFound();
            }

            return View(obavijest);
        }

        // POST: Obavijest/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var obavijest = await _context.Obavijest.FindAsync(id);
            if (obavijest != null)
            {
                _context.Obavijest.Remove(obavijest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ObavijestExistsAsync(int id)
        {
            return await _context.Obavijest.AnyAsync(e => e.Id == id);
        }
        
    }
}
