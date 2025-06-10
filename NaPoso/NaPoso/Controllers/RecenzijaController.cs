using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [Authorize(Roles = "Klijent,Admin")]
        public IActionResult Create(string radnikId, int? oglasId)
        {
            ViewBag.Debug_RouteParams = $"radnikId: {radnikId}, oglasId: {oglasId}";

            var verifiedOglasId = HttpContext.Session.GetInt32("VerifiedOglasId");
            var verifiedRadnikId = HttpContext.Session.GetString("VerifiedRadnikId");
            var paymentVerified = HttpContext.Session.GetString("PaymentVerified");

            ViewBag.Debug_Session = $"Session values - verifiedOglasId: {verifiedOglasId}, " +
                                   $"verifiedRadnikId: {verifiedRadnikId}, " +
                                   $"paymentToken: {(paymentVerified != null ? "exists" : "missing")}";

            if (!User.IsInRole("Admin"))
            {
                if (!oglasId.HasValue)
                {
                    TempData["ErrorMessage"] = "Nedostaje ID oglasa.";
                    return RedirectToAction("Index", "Home");
                }

                bool bypassVerification = false;

                if (!bypassVerification && (
                    verifiedOglasId == null ||
                    verifiedOglasId != oglasId ||
                    string.IsNullOrEmpty(verifiedRadnikId) ||
                    verifiedRadnikId != radnikId ||
                    string.IsNullOrEmpty(paymentVerified)))
                {
                    TempData["ErrorMessage"] = "Plaćanje nije potvrđeno za ovaj oglas.";
                    return RedirectToAction("Index", "Home");
                }

                // ✅ Spremi ID-ove u sesiju
                HttpContext.Session.SetInt32("VerifiedOglasId", oglasId.Value);
                HttpContext.Session.SetString("VerifiedRadnikId", radnikId);
            }

            var recenzija = new Recenzija { RadnikId = radnikId };
            return View(recenzija);
        }

        // POST: Recenzija/Create
        // POST: Recenzija/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Klijent,Admin")]
        public async Task<IActionResult> Create([Bind("Ocjena,Sadrzaj,RadnikId")] Recenzija recenzija, int? oglasId)
        {
            // Postavi KlijentId iz logovanog korisnika
            recenzija.KlijentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Ukloni validaciju za KlijentId (ručno postavljeno)
            ModelState.Remove("KlijentId");

            // Debug info
            TempData["Debug_PostParams"] = $"KlijentId: {recenzija.KlijentId}, RadnikId: {recenzija.RadnikId}, OglasId: {oglasId}";

            if (!User.IsInRole("Admin"))
            {
                bool bypassVerification = false;

                if (!bypassVerification)
                {
                    var verifiedOglasId = HttpContext.Session.GetInt32("VerifiedOglasId");
                    var verifiedRadnikId = HttpContext.Session.GetString("VerifiedRadnikId");
                    var paymentVerified = HttpContext.Session.GetString("PaymentVerified");

                    TempData["Debug_SessionValues"] = $"Session values - verifiedOglasId: {verifiedOglasId}, " +
                                           $"verifiedRadnikId: {verifiedRadnikId}, " +
                                           $"paymentToken: {(paymentVerified != null ? "exists" : "missing")}";

                    if (verifiedOglasId == null ||
                        verifiedOglasId != oglasId ||
                        string.IsNullOrEmpty(verifiedRadnikId) ||
                        string.IsNullOrEmpty(paymentVerified))
                    {
                        TempData["ErrorMessage"] = "Plaćanje nije potvrđeno za ovaj oglas.";
                        return RedirectToAction("Index", "Home");
                    }

                    // ✅ Provjeri da li se RadnikId iz forme podudara sa sesijom
                    if (recenzija.RadnikId != verifiedRadnikId)
                    {
                        TempData["ErrorMessage"] = "Neispravni podaci za radnika.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Očisti sesiju nakon validacije
                    HttpContext.Session.Remove("PaymentVerified");
                    HttpContext.Session.Remove("VerifiedOglasId");
                    HttpContext.Session.Remove("VerifiedRadnikId");
                }
            }
            else
            {
                // Admin mora ručno unijeti RadnikId kroz formu
                if (string.IsNullOrEmpty(recenzija.RadnikId))
                {
                    ModelState.AddModelError("RadnikId", "Radnik nije definisan.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(recenzija);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Recenzija je uspješno dodana.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    TempData["Debug_Error"] = $"Error: {ex.Message}";
                    ModelState.AddModelError("", "Greška prilikom spremanja recenzije.");
                }
            }
            else
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Debug_ModelErrors"] = errors;
            }

            return View(recenzija);
        }

        // GET: Recenzija/Edit/5
        [Authorize(Roles = "Admin,Klijent")]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ocjena,Sadrzaj,RadnikId,KlijentId")] Recenzija recenzija)
        {
            if (id != recenzija.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recenzija);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Recenzija.Any(e => e.Id == recenzija.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index)); // ili gdje god želiš redirect
            }
            return View(recenzija);
        }

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
            var radnikId = User.FindFirstValue(ClaimTypes.NameIdentifier); 

            var recenzije = await _context.Recenzija
                                          .Where(r => r.RadnikId == radnikId)
                                          .ToListAsync();
            var klijentiIds = recenzije.Select(r => r.KlijentId).Distinct().ToList();

            var klijenti = await _context.Korisnik
                                         .Where(u => klijentiIds.Contains(u.Id))
                                         .Select(u => new { u.Id, u.Email })
                                         .ToListAsync();

            var recenzijeSaEmailom = recenzije.Select(r => new {
                Recenzija = r,
                KlijentEmail = klijenti.FirstOrDefault(k => k.Id == r.KlijentId)?.Email ?? "Nepoznat"
            }).ToList();
            var model = recenzije.Select(r => new RecenzijaViewModel
            {
                KlijentEmail = klijenti.FirstOrDefault(k => k.Id == r.KlijentId)?.Email ?? "Nepoznat",
                Ocjena = r.Ocjena,
                Sadrzaj = r.Sadrzaj
            }).ToList();


            return View(model);
        }
    }
}