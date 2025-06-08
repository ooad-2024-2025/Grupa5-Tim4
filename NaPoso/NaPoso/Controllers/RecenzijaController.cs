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
    public class RecenzijaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecenzijaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recenzija
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Recenzija.ToListAsync());
        }

        // GET: Recenzija/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _context.Recenzija
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recenzija == null)
            {
                return NotFound();
            }

            return View(recenzija);
        }

        // GET: Recenzija/Create
        [Authorize(Roles = "Klijent,Admin")] // Fixed: removed space after comma
        public IActionResult Create(string radnikId)
        {
            var recenzija = new Recenzija { RadnikId = radnikId };
            return View(recenzija);
        }

        // POST: Recenzija/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Klijent,Admin")] // Fixed: removed space after comma
        public async Task<IActionResult> Create([Bind("Ocjena,Sadrzaj,RadnikId")] Recenzija recenzija)
        {
            // Set the client ID from the current user
            recenzija.KlijentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // FIXED: Remove KlijentId from ModelState validation since we set it manually
            ModelState.Remove("KlijentId");

            // DEBUG: Add this temporarily to see what's happening
            TempData["DebugInfo"] = $"RadnikId: {recenzija.RadnikId} | KlijentId: {recenzija.KlijentId} | ModelState.IsValid: {ModelState.IsValid}";

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["DebugErrors"] = $"Errors: {errors}";
            }

            // FIXED: Remove the contradictory logic - check if ModelState is valid, not invalid
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(recenzija);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it appropriately
                    ModelState.AddModelError("", "Dogodila se greška prilikom spremanja recenzije.");
                    return View(recenzija);
                }
            }

            // If we get here, ModelState is not valid - show validation errors
            return View(recenzija);
        }



        // GET: Recenzija/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _context.Recenzija.FindAsync(id);
            if (recenzija == null)
            {
                return NotFound();
            }
            return View(recenzija);
        }

        // POST: Recenzija/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        // GET: Recenzija/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _context.Recenzija
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recenzija == null)
            {
                return NotFound();
            }

            return View(recenzija);
        }

        // POST: Recenzija/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recenzija = await _context.Recenzija.FindAsync(id);
            if (recenzija != null)
            {
                _context.Recenzija.Remove(recenzija);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecenzijaExists(int id)
        {
            return _context.Recenzija.Any(e => e.Id == id);
        }
        [Authorize(Roles = "Radnik")]
        public async Task<IActionResult> MojeRecenzije()
        {
            var radnikId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Trenutni korisnik (Radnik)

            var recenzije = await _context.Recenzija
                                          .Where(r => r.RadnikId == radnikId)
                                          .ToListAsync();

            return View(recenzije);
        }
    }
}
