using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    [Authorize(Roles = RoleConstants.Admin)]
    public class OglasKorisnikController : Controller
    {
        
        private readonly ApplicationDbContext _context;

        public OglasKorisnikController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OglasKorisnik
        public async Task<IActionResult> Index()
        {
            return View(await _context.OglasKorisnik.ToListAsync());
        }

        // GET: OglasKorisnik/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglasKorisnik = await _context.OglasKorisnik
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oglasKorisnik == null)
            {
                return NotFound();
            }

            return View(oglasKorisnik);
        }

        // GET: OglasKorisnik/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: OglasKorisnik/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,KorisnikId")] OglasKorisnik oglasKorisnik)
        {
            if (ModelState.IsValid)
            {
                _context.Add(oglasKorisnik);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(oglasKorisnik);
        }

        // GET: OglasKorisnik/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglasKorisnik = await _context.OglasKorisnik.FindAsync(id);
            if (oglasKorisnik == null)
            {
                return NotFound();
            }
            return View(oglasKorisnik);
        }

        // POST: OglasKorisnik/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,KorisnikId")] OglasKorisnik oglasKorisnik)
        {
            if (id != oglasKorisnik.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(oglasKorisnik);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await OglasKorisnikExistsAsync(oglasKorisnik.Id))
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
            return View(oglasKorisnik);
        }

        // GET: OglasKorisnik/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglasKorisnik = await _context.OglasKorisnik
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oglasKorisnik == null)
            {
                return NotFound();
            }

            return View(oglasKorisnik);
        }

        // POST: OglasKorisnik/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var oglasKorisnik = await _context.OglasKorisnik.FindAsync(id);
            if (oglasKorisnik != null)
            {
                _context.OglasKorisnik.Remove(oglasKorisnik);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> OglasKorisnikExistsAsync(int id)
        {
            return await _context.OglasKorisnik.AnyAsync(e => e.Id == id);
        }
    }
}
