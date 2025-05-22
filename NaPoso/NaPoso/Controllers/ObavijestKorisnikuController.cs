using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
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

        // GET: ObavijestKorisniku/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: ObavijestKorisniku/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ObavijestKorisniku/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
    }
}
