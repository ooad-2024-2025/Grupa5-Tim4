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
        private readonly ILogger<RecenzijaController> _logger;

        public RecenzijaController(ApplicationDbContext context, IRecenzijaService recenzijaService, ILogger<RecenzijaController> logger)
        {
            _context = context;
            _recenzijaService = recenzijaService;
            _logger = logger;
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "[Recenzija GET Create] Ulaz: UserId={CurrUser}, RadnikId(route)='{RId}', OglasId(route)={OId}, " +
                "Session_VerifiedOglasId={SOId}, Session_VerifiedRadnikId='{SRId}', Session_PaymentVerified='{SPV}'",
                currentUserId, radnikId ?? "<NULL>", oglasId.HasValue ? oglasId.Value.ToString() : "<NULL>",
                verifiedOglasId.HasValue ? verifiedOglasId.Value.ToString() : "<NULL>",
                verifiedRadnikId ?? "<NULL>",
                paymentVerified ?? "<NULL>");

            if (!User.IsInRole(RoleConstants.Admin))
            {
                if (!oglasId.HasValue)
                {
                    _logger.LogWarning("[Recenzija GET Create] OglasId(route) je NULL/empty — redirect na Home!");
                    TempData["ErrorMessage"] = "Nedostaje ID oglasa.";
                    return RedirectToAction("Index", "Home");
                }

                // --- 1) Pokušaj SESSION provjeru (brza, ali može nestati u Dockeru) ---
                bool sessionOk = verifiedOglasId.HasValue &&
                                 verifiedOglasId == oglasId &&
                                 !string.IsNullOrEmpty(verifiedRadnikId) &&
                                 verifiedRadnikId == radnikId &&
                                 !string.IsNullOrEmpty(paymentVerified);

                // --- 2) FALLBACK DB provjera: provjeri da li postoji PLAĆENA transakcija za ovaj oglas
                //     čiji je vlasnik (UserId) trenutno prijavljeni klijent. Ovo je pouzdanija
                //     provjera jer ne ovisi o sesijskom skladištu.
                bool dbOk = false;
                if (!sessionOk)
                {
                    var transakcija = _context.PaymentTransactions
                        .FirstOrDefault(pt =>
                            pt.OglasId == oglasId.Value &&
                            pt.UserId == currentUserId &&
                            (pt.Status == PaymentStatus.Paid ||
                             pt.Status == PaymentStatus.Released ||
                             pt.Status == PaymentStatus.Held));

                    if (transakcija != null)
                    {
                        dbOk = true;
                        _logger.LogInformation(
                            "[Recenzija GET Create] DB provjera [PaymentTransaction] USPJELA! TxId={TxId}, Status={Status}, " +
                            "UserId={TxUser}, OglasId={TxOglas}, WorkerUserId={TxWorker}",
                            transakcija.Id, transakcija.Status, transakcija.UserId,
                            transakcija.OglasId, transakcija.WorkerUserId);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[Recenzija GET Create] DB provjera [PaymentTransaction] prazna. " +
                            "Pokušavam FALLBACK [OglasKorisnik.Status == Zavrsen/Placen]...");

                        // --- 2b) DODATNI FALLBACK: slučaj da PaymentTransaction nije upisana
                        //        (bug u webhooku/Success) ali je Oglas OZNAČEN kao Završen/Placen
                        //        za ovog klijenta i radnika. To znači da je Stripe naplatio,
                        //        pa ćemo i dalje dozvoliti recenziju.
                        var prijava = _context.OglasKorisnik
                            .FirstOrDefault(ok =>
                                ok.OglasId == oglasId.Value &&
                                !string.IsNullOrEmpty(radnikId) && ok.KorisnikId == radnikId &&
                                (ok.Status == Enums.Enums.Status.Zavrsen ||
                                 ok.Status == Enums.Enums.Status.Placen));

                        // Ako radnikId nije ni proslijeđen u route-u, nađi ga u OglasKorisnik
                        if (prijava == null && string.IsNullOrEmpty(radnikId))
                        {
                            prijava = _context.OglasKorisnik
                                .FirstOrDefault(ok =>
                                    ok.OglasId == oglasId.Value &&
                                    (ok.Status == Enums.Enums.Status.Zavrsen ||
                                     ok.Status == Enums.Enums.Status.Placen));
                            if (prijava != null)
                            {
                                radnikId = prijava.KorisnikId;
                                _logger.LogInformation(
                                    "[Recenzija GET Create] RadnikId nije bio u route-u, popunjen iz OglasKorisnik: {RId}",
                                    radnikId);
                            }
                        }

                        if (prijava != null)
                        {
                            dbOk = true;
                            _logger.LogInformation(
                                "[Recenzija GET Create] DB provjera [OglasKorisnik] USPJELA! OglasKorisnikId={OKId}, " +
                                "Status={Status}, OglasId={OId}, KorisnikId(Radnik)={KId}",
                                prijava.Id, prijava.Status, prijava.OglasId, prijava.KorisnikId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[Recenzija GET Create] DB provjera [OglasKorisnik] NIJE USPJELA! " +
                                "Za OglasId={OId} i RadnikId(route)='{RId}' ne postoji prijava sa Zavrsen/Placen.",
                                oglasId.Value, radnikId ?? "<NULL>");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("[Recenzija GET Create] SESSION provjera je prošla odmah.");
                }

                _logger.LogInformation(
                    "[Recenzija GET Create] Provjere: sessionOk={SOk}, dbOk={DOk}, final_result={FinalOk}",
                    sessionOk, dbOk, sessionOk || dbOk);

                if (!sessionOk && !dbOk)
                {
                    TempData["ErrorMessage"] = "Plaćanje nije potvrđeno za ovaj oglas.";
                    return RedirectToAction("Index", "Home");
                }

                // Ako je DB provjera prošla, obnovi sesijske varijable za sljedeći korak (POST)
                if (dbOk && !sessionOk)
                {
                    HttpContext.Session.SetString("PaymentVerified", "true");
                    HttpContext.Session.SetInt32("VerifiedOglasId", oglasId.Value);
                    HttpContext.Session.SetString("VerifiedRadnikId", radnikId);
                    _logger.LogInformation("[Recenzija GET Create] Session varijable obnovljene (DB fallback).");
                }
                else
                {
                    HttpContext.Session.SetInt32("VerifiedOglasId", oglasId.Value);
                    HttpContext.Session.SetString("VerifiedRadnikId", radnikId);
                }
            }

            var recenzija = new Recenzija { RadnikId = radnikId };
            _logger.LogInformation("[Recenzija GET Create] Prikazujem View. Recenzija.RadnikId='{RId}'", radnikId ?? "<NULL>");
            return View(recenzija);
        }

        // POST: Recenzija/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Klijent + "," + RoleConstants.Admin)]
        public async Task<IActionResult> Create([Bind("Ocjena,Sadrzaj,RadnikId")] Recenzija recenzija, int? oglasId)
        {
            // Postavi KlijentId iz logovanog korisnika
            recenzija.KlijentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = recenzija.KlijentId;

            _logger.LogInformation(
                "[Recenzija POST Create] Ulaz: UserId={CurrUser}, OglasId(route)={OId}, Recenzija.RadnikId='{RId}', Ocjena={Oc}",
                currentUserId, oglasId.HasValue ? oglasId.Value.ToString() : "<NULL>",
                recenzija.RadnikId ?? "<NULL>", recenzija.Ocjena);

            // Ukloni validaciju za KlijentId (ručno postavljeno)
            ModelState.Remove("KlijentId");

            if (!User.IsInRole(RoleConstants.Admin))
            {
                var verifiedOglasId = HttpContext.Session.GetInt32("VerifiedOglasId");
                var verifiedRadnikId = HttpContext.Session.GetString("VerifiedRadnikId");
                var paymentVerified = HttpContext.Session.GetString("PaymentVerified");

                _logger.LogInformation(
                    "[Recenzija POST Create] Session: VOId={SOId}, VRId='{SRId}', PV='{SPV}'",
                    verifiedOglasId.HasValue ? verifiedOglasId.Value.ToString() : "<NULL>",
                    verifiedRadnikId ?? "<NULL>", paymentVerified ?? "<NULL>");

                // --- 1) SESSION provjera ---
                bool sessionOk = verifiedOglasId.HasValue &&
                                 verifiedOglasId == oglasId &&
                                 !string.IsNullOrEmpty(verifiedRadnikId) &&
                                 !string.IsNullOrEmpty(paymentVerified);

                // --- 2) FALLBACK DB provjera ---
                bool dbOk = false;
                if (!sessionOk)
                {
                    var transakcija = oglasId.HasValue ? _context.PaymentTransactions
                        .FirstOrDefault(pt =>
                            pt.OglasId == oglasId.Value &&
                            pt.UserId == currentUserId &&
                            (pt.Status == PaymentStatus.Paid ||
                             pt.Status == PaymentStatus.Released ||
                             pt.Status == PaymentStatus.Held)) : null;

                    if (transakcija != null)
                    {
                        dbOk = true;
                        _logger.LogInformation(
                            "[Recenzija POST Create] DB provjera [PaymentTransaction] USPJELA! TxId={TxId}, Status={Status}",
                            transakcija.Id, transakcija.Status);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[Recenzija POST Create] DB provjera [PaymentTransaction] prazna. " +
                            "Pokušavam FALLBACK [OglasKorisnik.Status == Zavrsen/Placen]...");

                        // --- 2b) FALLBACK: PaymentTransaction možda nije upisana (bug),
                        //        ali je OglasKorisnik status već Zavrsen → dozvoli recenziju.
                        var prijava = oglasId.HasValue ? _context.OglasKorisnik
                            .FirstOrDefault(ok =>
                                ok.OglasId == oglasId.Value &&
                                !string.IsNullOrEmpty(recenzija.RadnikId) && ok.KorisnikId == recenzija.RadnikId &&
                                (ok.Status == Enums.Enums.Status.Zavrsen ||
                                 ok.Status == Enums.Enums.Status.Placen)) : null;

                        if (prijava == null && string.IsNullOrEmpty(recenzija.RadnikId) && oglasId.HasValue)
                        {
                            prijava = _context.OglasKorisnik
                                .FirstOrDefault(ok =>
                                    ok.OglasId == oglasId.Value &&
                                    (ok.Status == Enums.Enums.Status.Zavrsen ||
                                     ok.Status == Enums.Enums.Status.Placen));
                            if (prijava != null)
                            {
                                recenzija.RadnikId = prijava.KorisnikId;
                                _logger.LogInformation(
                                    "[Recenzija POST Create] RadnikId je prazan u formi, popunjen iz OglasKorisnik: {RId}",
                                    recenzija.RadnikId);
                            }
                        }

                        if (prijava != null)
                        {
                            dbOk = true;
                            _logger.LogInformation(
                                "[Recenzija POST Create] DB provjera [OglasKorisnik] USPJELA! " +
                                "OglasKorisnikId={OKId}, Status={Status}, OglasId={OId}",
                                prijava.Id, prijava.Status, prijava.OglasId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[Recenzija POST Create] DB provjera [OglasKorisnik] NIJE USPJELA! " +
                                "OglasId={OId}, RadnikId='{RId}'",
                                oglasId.HasValue ? oglasId.Value.ToString() : "<NULL>",
                                recenzija.RadnikId ?? "<NULL>");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("[Recenzija POST Create] SESSION provjera prošla.");
                }

                _logger.LogInformation(
                    "[Recenzija POST Create] Rezultat: sessionOk={SOk}, dbOk={DOk}, final={FinalOk}",
                    sessionOk, dbOk, sessionOk || dbOk);

                if (!sessionOk && !dbOk)
                {
                    TempData["ErrorMessage"] = "Plaćanje nije potvrđeno za ovaj oglas.";
                    return RedirectToAction("Index", "Home");
                }

                // Provjeri da li se RadnikId iz forme podudara sa sesijom (ako je sesija ispravna)
                if (sessionOk && recenzija.RadnikId != verifiedRadnikId)
                {
                    _logger.LogWarning(
                        "[Recenzija POST Create] RadnikId mismatch! Session_RadnikId='{S}', Form_RadnikId='{F}'",
                        verifiedRadnikId, recenzija.RadnikId);
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
                    _logger.LogInformation("[Recenzija POST Create] Recenzija kreirana! Id={RId}, KlijentId={KId}, RadnikId={RId}",
                        recenzija.Id, recenzija.KlijentId, recenzija.RadnikId);
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Recenzija POST Create] Greška prilikom CreateAsync.");
                    ModelState.AddModelError("", "Greška prilikom spremanja recenzije.");
                }
            }
            else
            {
                _logger.LogWarning("[Recenzija POST Create] ModelState.Invalid! Errors: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
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