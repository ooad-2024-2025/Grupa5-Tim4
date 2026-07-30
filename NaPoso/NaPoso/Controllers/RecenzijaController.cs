using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NaPoso.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;

namespace NaPoso.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class RecenzijaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecenzijaService _recenzijaService;

        public RecenzijaController(ApplicationDbContext context, IRecenzijaService recenzijaService)
        {
            _context = context;
            _recenzijaService = recenzijaService;
        }

        // GET: Recenzija
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent + "," + RoleConstants.Radnik)]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            
            var query = _context.Recenzija.AsQueryable();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Filtriraj logično po ulozi!
            if (User.IsInRole(RoleConstants.Klijent))
            {
                // Klijent vidi samo recenzije koje je on kreirao/ostavio
                query = query.Where(r => r.KlijentId == userId);
            }
            else if (User.IsInRole(RoleConstants.Radnik))
            {
                // Radnik vidi samo recenzije koje su klijenti ostavili njemu
                query = query.Where(r => r.RadnikId == userId);
            }
            // Admin vidi sve recenzije (query ostaje nepromijenjen)

            var recenzije = await query
                .OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return View(recenzije);
        }

        // GET: Recenzija/Details/5
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent + "," + RoleConstants.Radnik)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _recenzijaService.GetByIdAsync(id.Value);
            if (recenzija == null)
            {
                return NotFound();
            }

            return View(recenzija);
        }

        // GET: Recenzija/Create
        [Authorize(Roles = RoleConstants.Klijent + "," + RoleConstants.Admin)]
        public IActionResult Create(string radnikId, int? oglasId)
        {
            var verifiedOglasId = HttpContext.Session.GetInt32("VerifiedOglasId");
            var verifiedRadnikId = HttpContext.Session.GetString("VerifiedRadnikId");
            var paymentVerified = HttpContext.Session.GetString("PaymentVerified");

            if (!User.IsInRole(RoleConstants.Admin))
            {
                if (!oglasId.HasValue)
                {
                    TempData["ErrorMessage"] = "Nedostaje ID oglasa.";
                    return RedirectToAction("Index", "Home");
                }

                if (verifiedOglasId == null ||
                    verifiedOglasId != oglasId ||
                    string.IsNullOrEmpty(verifiedRadnikId) ||
                    verifiedRadnikId != radnikId ||
                    string.IsNullOrEmpty(paymentVerified))
                {
                    TempData["ErrorMessage"] = "Plaćanje nije potvrđeno za ovaj oglas.";
                    return RedirectToAction("Index", "Home");
                }


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
        [Authorize(Roles = RoleConstants.Klijent + "," + RoleConstants.Admin)]
        public async Task<IActionResult> Create([Bind("Ocjena,Sadrzaj,RadnikId")] Recenzija recenzija, int? oglasId)
        {
            // Postavi KlijentId iz logovanog korisnika
            recenzija.KlijentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Ukloni validaciju za KlijentId (ručno postavljeno)
            ModelState.Remove("KlijentId");

            if (!User.IsInRole(RoleConstants.Admin))
            {
                var verifiedOglasId = HttpContext.Session.GetInt32("VerifiedOglasId");
                var verifiedRadnikId = HttpContext.Session.GetString("VerifiedRadnikId");
                var paymentVerified = HttpContext.Session.GetString("PaymentVerified");

                if (verifiedOglasId == null ||
                    verifiedOglasId != oglasId ||
                    string.IsNullOrEmpty(verifiedRadnikId) ||
                    string.IsNullOrEmpty(paymentVerified))
                {
                    TempData["ErrorMessage"] = "Plaćanje nije potvrđeno za ovaj oglas.";
                    return RedirectToAction("Index", "Home");
                }

                // Provjeri da li se RadnikId iz forme podudara sa sesijom
                if (recenzija.RadnikId != verifiedRadnikId)
                {
                    TempData["ErrorMessage"] = "Neispravni podaci za radnika.";
                    return RedirectToAction("Index", "Home");
                }

                // Ocisti sesiju nakon validacije
                HttpContext.Session.Remove("PaymentVerified");
                HttpContext.Session.Remove("VerifiedOglasId");
                HttpContext.Session.Remove("VerifiedRadnikId");
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
                    await _recenzijaService.CreateAsync(recenzija);

                    TempData["SuccessMessage"] = "Recenzija je uspješno dodana.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Greška prilikom spremanja recenzije.");
                }
            }

            return View(recenzija);
        }

        // GET: Recenzija/Edit/5
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _recenzijaService.GetByIdAsync(id.Value);

            if (recenzija == null)
            {
                return NotFound();
            }

            return View(recenzija);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin)]
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
                    var updated = await _recenzijaService.UpdateAsync(id, recenzija);
                    if (updated == null)
                    {
                        return NotFound();
                    }
                }
                catch (Exception)
                {
                    if (!await _recenzijaService.ExistsAsync(recenzija.Id))
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
            return View(recenzija);
        }

        // GET: Recenzija/Delete/5
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _recenzijaService.GetByIdAsync(id.Value);
            if (recenzija == null)
            {
                return NotFound();
            }

            return View(recenzija);
        }

        // POST: Recenzija/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _recenzijaService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> RecenzijaExistsAsync(int id)
        {
            return await _recenzijaService.ExistsAsync(id);
        }

        [Authorize(Roles = RoleConstants.Radnik)]
        public async Task<IActionResult> MojeRecenzije()
        {
            var radnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = await _recenzijaService.GetByRadnikIdWithEmailAsync(radnikId);
            return View(model);
        }
    }
}