﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
                oglas.Status = Status.Aktivan;
                oglas.RadnikId = null;

                _context.Add(oglas);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(OglasiKlijenta));
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Opis,Lokacija,TipPosla,CijenaPosla,Naslov,Status")] Oglas oglas)
        {
            if (id != oglas.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var oglasIzBaze = await _context.Oglas.FindAsync(id);
                    if (oglasIzBaze == null)
                    {
                        return NotFound();
                    }

                    // Ažuriraj samo potrebna polja
                    oglasIzBaze.Opis = oglas.Opis;
                    oglasIzBaze.Lokacija = oglas.Lokacija;
                    oglasIzBaze.TipPosla = oglas.TipPosla;
                    oglasIzBaze.CijenaPosla = oglas.CijenaPosla;
                    oglasIzBaze.Naslov = oglas.Naslov;

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
                return RedirectToAction(nameof(Edit), new { id = oglas.Id });
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
            return RedirectToAction(nameof(OglasiKlijenta));
        }

        private bool OglasExists(int id)
        {
            return _context.Oglas.Any(e => e.Id == id);
        }
        [Authorize(Roles = "Radnik")]
        public async Task<IActionResult> PrikazOglasa()
        {
            var oglasi = await _context.Oglas
                .Where(o => o.Status == Status.Aktivan && o.RadnikId == null)
                .ToListAsync();
            return View(oglasi);
        }
        [Authorize(Roles = "Klijent")]
        public async Task<IActionResult> OglasiKlijenta()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var oglasi = await _context.Oglas
                .Where(o => o.KlijentId == userId)
                .ToListAsync();

            return View(oglasi);
        }
        [Authorize(Roles = "Klijent")]
        public async Task<IActionResult> PrijavljeniRadnici(int oglasId)
        {
            // Prvo dohvatimo oglas i proverimo da li klijent koji gleda je vlasnik oglasa
            var oglas = await _context.Oglas
                .FirstOrDefaultAsync(o => o.Id == oglasId);

            if (oglas == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (oglas.KlijentId != userId)
                return Forbid();

            // Dohvati prijave za taj oglas, ukljucujuci informacije o radniku (korisniku)
            var prijave = await _context.OglasKorisnik
                .Where(ok => ok.OglasId == oglasId)
                .Include(ok => ok.Korisnik) // ako imas navigacionu property za korisnika
                .ToListAsync();

            return View(prijave);
        }
        [Authorize(Roles = "Radnik")]
        public async Task<IActionResult> PrijaviRadnikaNaOglas(int oglasId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Provjera da li se već prijavio
            var postoji = await _context.OglasKorisnik
                .AnyAsync(ok => ok.OglasId == oglasId && ok.KorisnikId == userId);

            if (!postoji)
            {
                var prijava = new Models.OglasKorisnik
                {
                    OglasId = oglasId,
                    KorisnikId = userId,
                    Status = Status.Aktivan
                };

                _context.OglasKorisnik.Add(prijava);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("PrikazOglasa");
        }

        [Authorize(Roles = "Radnik")]
        public async Task<IActionResult> PrijaviSe(int oglasId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Provjera da li se već prijavio
            var prijava = new OglasKorisnik
            {
                KorisnikId = userId,
                OglasId = oglasId,
                Status = Status.Aktivan
            };

            _context.OglasKorisnik.Add(prijava);
            await _context.SaveChangesAsync();

            return RedirectToAction("MojiOglasi");
        }
        [Authorize(Roles = "Klijent")]
        public async Task<IActionResult> Prihvati(int id)
        {
            var prijava = await _context.OglasKorisnik
                .Include(p => p.Oglas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prijava == null || prijava.Oglas == null)
                return NotFound();

            // Dodaj radnika u oglas i promijeni status
            prijava.Oglas.RadnikId = prijava.KorisnikId;
            prijava.Oglas.Status = Status.Neaktivan;

            await _context.SaveChangesAsync();

            return RedirectToAction("PrijavljeniRadnici", new { oglasId = prijava.Id });
        }

        [Authorize(Roles = "Klijent")]
        public async Task<IActionResult> Odbij(int id)
        {
            var prijava = await _context.OglasKorisnik
                .Include(p => p.Oglas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prijava == null || prijava.Oglas == null)
                return NotFound();

            // Možeš obrisati prijavu, ili joj promijeniti status
            _context.OglasKorisnik.Remove(prijava);
            await _context.SaveChangesAsync();

            return RedirectToAction("PrijavljeniRadnici", new { oglasId = prijava.Id });
        }
        
    }
}
