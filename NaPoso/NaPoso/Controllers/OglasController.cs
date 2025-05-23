using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using SQLitePCL;
using static NaPoso.Enums.Enums;

namespace NaPoso.Controllers
{
    public class OglasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OglasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Oglas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Oglas.ToListAsync());
        }

        // GET: Oglas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _context.Oglas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oglas == null)
            {
                return NotFound();
            }

            return View(oglas);
        }

        // GET: Oglas/Create
        public IActionResult Create()
        {
            return View();
        }

        //POST: Oglas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Opis,Lokacija,TipPosla,CijenaPosla,Naslov")] Oglas oglas)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine(error);  // Ili logiraj negdje
                }
                return View(oglas);
            }
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    // Nema usera, vrati neki error ili redirect na login
                    return Unauthorized();
                }
                oglas.KlijentId = userId;
                oglas.Status = Status.AktivanOglas;
                oglas.RadnikId = null;

                _context.Add(oglas);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(oglas);
        }

        // GET: Oglas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _context.Oglas.FindAsync(id);
            if (oglas == null)
            {
                return NotFound();
            }
            return View(oglas);
        }

        // POST: Oglas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,KlijentId,RadnikId,Opis,Lokacija,TipPosla,CijenaPosla,Naslov,Status")] Oglas oglas)
        {
            if (id != oglas.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(oglas);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OglasExists(oglas.Id))
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
            return View(oglas);
        }

        // GET: Oglas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _context.Oglas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oglas == null)
            {
                return NotFound();
            }

            return View(oglas);
        }

        // POST: Oglas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var oglas = await _context.Oglas.FindAsync(id);
            if (oglas != null)
            {
                _context.Oglas.Remove(oglas);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OglasExists(int id)
        {
            return _context.Oglas.Any(e => e.Id == id);
        }
    }
}
