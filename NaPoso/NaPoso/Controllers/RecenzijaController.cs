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
            // Create debug data to see what's happening
            ViewBag.Debug_RouteParams = $"radnikId: {radnikId}, oglasId: {oglasId}";

            // Session values 
            var verifiedOglasId = HttpContext.Session.GetInt32("VerifiedOglasId");
            var verifiedRadnikId = HttpContext.Session.GetString("VerifiedRadnikId");
            var paymentVerified = HttpContext.Session.GetString("PaymentVerified");

            ViewBag.Debug_Session = $"Session values - verifiedOglasId: {verifiedOglasId}, " +
                                   $"verifiedRadnikId: {verifiedRadnikId}, " +
                                   $"paymentToken: {(paymentVerified != null ? "exists" : "missing")}";

            // Admin can bypass verification
            if (!User.IsInRole("Admin"))
            {
                // Check if this is coming from a verified payment
                if (!oglasId.HasValue)
                {
                    TempData["ErrorMessage"] = "Nedostaje ID oglasa.";
                    return RedirectToAction("Index", "Home");
                }

                // TEMPORARY - REMOVE IN PRODUCTION: Allow all requests for debugging
                bool bypassVerification = false; // Set to true to bypass verification temporarily

                // Validate that this request matches verified payment data
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
            }

            var recenzija = new Recenzija { RadnikId = radnikId };
            return View(recenzija);
        }

        // POST: Recenzija/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Klijent,Admin")]
        public async Task<IActionResult> Create([Bind("Ocjena,Sadrzaj,RadnikId")] Recenzija recenzija, int? oglasId)
        {
            // Set the client ID from the current user
            recenzija.KlijentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Debug info
            TempData["Debug_PostParams"] = $"KlijentId: {recenzija.KlijentId}, RadnikId: {recenzija.RadnikId}, OglasId: {oglasId}";

            // Verify payment for clients
            if (!User.IsInRole("Admin"))
            {
                // TEMPORARY - REMOVE IN PRODUCTION: Allow all requests for debugging
                bool bypassVerification = false; // Set to true to bypass verification temporarily

                if (!bypassVerification)
                {
                    // Get verification data from session
                    var verifiedOglasId = HttpContext.Session.GetInt32("VerifiedOglasId");
                    var verifiedRadnikId = HttpContext.Session.GetString("VerifiedRadnikId");
                    var paymentVerified = HttpContext.Session.GetString("PaymentVerified");

                    TempData["Debug_SessionValues"] = $"Session values - verifiedOglasId: {verifiedOglasId}, " +
                                       $"verifiedRadnikId: {verifiedRadnikId}, " +
                                       $"paymentToken: {(paymentVerified != null ? "exists" : "missing")}";

                    // Validate that this submission matches verified payment data
                    if (verifiedOglasId == null ||
                        verifiedOglasId != oglasId ||
                        string.IsNullOrEmpty(verifiedRadnikId) ||
                        verifiedRadnikId != recenzija.RadnikId ||
                        string.IsNullOrEmpty(paymentVerified))
                    {
                        TempData["ErrorMessage"] = "Plaćanje nije potvrđeno za ovaj oglas.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Clear the verification after use
                    HttpContext.Session.Remove("PaymentVerified");
                    HttpContext.Session.Remove("VerifiedOglasId");
                    HttpContext.Session.Remove("VerifiedRadnikId");
                }
            }

            ModelState.Remove("KlijentId");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(recenzija);
                    await _context.SaveChangesAsync();

                    // Add success message
                    TempData["SuccessMessage"] = "Recenzija je uspješno dodana.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    TempData["Debug_Error"] = $"Error: {ex.Message}";
                    ModelState.AddModelError("", "Dogodila se greška prilikom spremanja recenzije.");
                    return View(recenzija);
                }
            }
            else
            {
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
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
            var radnikId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Trenutni korisnik (Radnik)

            var recenzije = await _context.Recenzija
                                          .Where(r => r.RadnikId == radnikId)
                                          .ToListAsync();

            return View(recenzije);
        }
    }
}