using System;
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
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;
using Microsoft.AspNetCore.Identity;
using Korisnik = NaPoso.Models.Korisnik;


namespace NaPoso.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class OglasController : Controller
    {
        private readonly IOglasService _oglasService;
        private readonly UserManager<Korisnik> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly PaymentTransactionService _paymentService;
        private readonly IStripeConnectService _stripeConnectService;
        private readonly ILogger<OglasController> _logger;

        public OglasController(
            UserManager<Korisnik> userManager, 
            IOglasService oglasService, 
            ApplicationDbContext context,
            PaymentTransactionService paymentService,
            IStripeConnectService stripeConnectService,
            ILogger<OglasController> logger)
        {
            _oglasService = oglasService;
            _userManager = userManager;
            _context = context;
            _paymentService = paymentService;
            _stripeConnectService = stripeConnectService;
            _logger = logger;
        }

        [Authorize(Roles = RoleConstants.Admin)]
        // GET: Oglas
        public async Task<IActionResult> Index()
        {
            return View(await _oglasService.GetAllOglasAsync());
        }

        // GET: Oglas/Details/5
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent + "," + RoleConstants.Radnik)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _oglasService.GetOglasByIdAsync(id.Value);
            if (oglas == null)
            {
                return NotFound();
            }

            if (User.IsInRole(RoleConstants.Radnik))
            {
                var radnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var vecPrijavljen = await _context.OglasKorisnik
                    .AnyAsync(ok => ok.OglasId == id.Value && ok.KorisnikId == radnikId);
                ViewBag.VecPrijavljen = vecPrijavljen;
            }

            return View(oglas);
        }

        // GET: Oglas/Create
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public IActionResult Create()
        {
            return View();
        }

        //POST: Oglas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> Create([Bind("Opis,Lokacija,TipPosla,CijenaPosla,Naslov")] Oglas oglas)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }
                return View(oglas);
            }
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                string autorUloga;
                string redirectAkcija;
                if (User.IsInRole(RoleConstants.Admin))
                {
                    autorUloga = RoleConstants.Klijent;
                    redirectAkcija = nameof(Index);
                }
                else
                {
                    autorUloga = RoleConstants.Klijent;
                    redirectAkcija = nameof(OglasiKlijenta);
                }

                await _oglasService.CreateOglasAsync(oglas, userId, autorUloga);
                TempData["ToastMessage"] = $"Oglas \"{oglas.Naslov}\" je uspješno kreiran.";
                return RedirectToAction(redirectAkcija);
            }
            return View(oglas);
        }

        // GET: Oglas/Edit/5
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _oglasService.GetOglasByIdAsync(id.Value);
            if (oglas == null)
            {
                return NotFound();
            }

            var korisnikId = _userManager.GetUserId(User);
            bool jeVlasnik = oglas.KlijentId == korisnikId || oglas.RadnikId == korisnikId;
            if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
            {
                return Forbid();
            }

            return View(oglas);
        }

        // POST: Oglas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Opis,Lokacija,TipPosla,CijenaPosla,Naslov,Status")] Oglas oglas)
        {
            var oglasIzBaze = await _oglasService.GetOglasByIdAsync(id);
            if (oglasIzBaze == null)
            {
                return NotFound();
            }

            var korisnikId = _userManager.GetUserId(User);
            bool jeVlasnik = oglasIzBaze.KlijentId == korisnikId || oglasIzBaze.RadnikId == korisnikId;
            if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
            {
                return Forbid();
            }

            if (id != oglas.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _oglasService.UpdateOglasAsync(id, oglas);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _oglasService.OglasExistsAsync(oglas.Id))
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
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _oglasService.GetOglasByIdAsync(id.Value);

            if (oglas == null)
            {
                return NotFound();
            }

            var korisnikId = _userManager.GetUserId(User);
            bool jeVlasnik = oglas.KlijentId == korisnikId || oglas.RadnikId == korisnikId;
            if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
            {
                return Forbid();
            }

            return View(oglas);
        }
        // POST: Oglas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var oglas = await _oglasService.GetOglasByIdAsync(id);
            var korisnikId = _userManager.GetUserId(User);

            bool jeVlasnik = oglas?.KlijentId == korisnikId || oglas?.RadnikId == korisnikId;
            if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
                return Forbid();

            if (oglas != null)
            {
                await _oglasService.DeleteOglasAsync(id);
            }

            if (User.IsInRole(RoleConstants.Admin))
            {
                return RedirectToAction(nameof(Index));
            }
            else if (User.IsInRole(RoleConstants.Klijent))
            {
                return RedirectToAction("OglasiKlijenta");
            }

           
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> Zavrsi(int id)
        {
            var oglas = await _oglasService.GetOglasByIdAsync(id);
            if (oglas == null)
            {
                return NotFound();
            }

            var korisnikId = _userManager.GetUserId(User);
            bool jeVlasnik = oglas.KlijentId == korisnikId || oglas.RadnikId == korisnikId;
            if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
            {
                return Forbid();
            }

            oglas.Status = Status.Zavrsen;
            await _oglasService.UpdateOglasAsync(id, oglas);

            // Ažuriraj status odabranog radnika (prijave) na Zavrsen
            var oglasKorisnik = await _context.OglasKorisnik
                .FirstOrDefaultAsync(ok => ok.OglasId == id && (ok.Status == Status.Prihvacen || ok.Status == Status.Placen));

            if (oglasKorisnik != null)
            {
                oglasKorisnik.Status = Status.Zavrsen;
                _context.OglasKorisnik.Update(oglasKorisnik);
                
                // Pronađi korisnika
                var radnik = await _userManager.FindByIdAsync(oglasKorisnik.KorisnikId!);

                // Payout automatizacija (samo za Klijenta koji vrsi placanje)
                // Radnik ne moze inicirati payout; samo mijenja status oglasa kao zavrsen
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OglasId == id && t.Status == PaymentStatus.Paid);

                if (transaction != null && User.IsInRole(RoleConstants.Klijent)
                    && radnik != null && radnik.PayoutsEnabled && !string.IsNullOrEmpty(radnik.StripeConnectedAccountId))
                {
                    try
                    {
                        long osnovicaFeninga = (long)Math.Round(Convert.ToDecimal(oglas.CijenaPosla) * 100);
                        if (osnovicaFeninga < 0) osnovicaFeninga = 0;
                        if (osnovicaFeninga > transaction.Amount) osnovicaFeninga = transaction.Amount;
                        var platformFee = (long)Math.Round(osnovicaFeninga * 0.10);
                        var workerAmount = transaction.Amount - platformFee;

                        var transfer = await _stripeConnectService.CreateTransferAsync(
                            radnik.StripeConnectedAccountId,
                            workerAmount,
                            transaction.Currency,
                            transaction.StripePaymentIntentId);

                        if (transfer != null)
                        {
                            transaction.TransferId = transfer.Id;
                        }
                        
                        transaction.Status = PaymentStatus.Released;
                        transaction.PlatformFeeAmount = platformFee;
                        transaction.WorkerUserId = radnik.Id;
                        transaction.UpdatedAt = DateTime.UtcNow;

                        _context.PaymentTransactions.Update(transaction);

                        // Pošalji obavještenje
                        var formattedAmount = $"{workerAmount / 100.0:F2} {transaction.Currency.ToUpper()}";
                        _context.Obavijest.Add(new Obavijest
                        {
                            KorisnikId = radnik.Id,
                            Sadrzaj = $"Klijent je označio posao \"{oglas.Naslov}\" kao završen. Iznos od {formattedAmount} je prebačen na vaš račun.",
                            VrijemeSlanja = DateTime.UtcNow,
                            Tip = Obavjestenje.DrugaObavjestenja
                        });

                        TempData["ToastMessage"] = $"Posao uspješno završen. Isplata od {formattedAmount} poslana radniku.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Automated payout failed for Oglas {OglasId}", id);
                        TempData["ToastMessage"] = "Posao završen, ali isplata nije uspjela. Admin je obaviješten.";
                    }
                }
                else
                {
                    // Dodaj obavijest za radnika iako nije Stripe
                    if (radnik != null)
                    {
                        _context.Obavijest.Add(new Obavijest
                        {
                            KorisnikId = radnik.Id,
                            Sadrzaj = $"Klijent je označio posao \"{oglas.Naslov}\" kao završen.",
                            VrijemeSlanja = DateTime.UtcNow,
                            Tip = Obavjestenje.DrugaObavjestenja
                        });
                    }
                    TempData["ToastMessage"] = "Posao uspješno završen.";
                }

                // NAJVAŽNIJE: Spremi promjene na OglasKorisnik i transakciju UVIJEK
                // (bez obzira da li je Stripe radio ili ne)
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["ToastMessage"] = "Posao uspješno završen.";
            }

            return RedirectToAction(nameof(OglasiKlijenta));
        }

        // ============================================================
        // AKCIJA: MojiOglasi — samo Klijent i Admin mogu vidjeti svoje objave
        // Koristi IOglasService.GetOglasByAutorIdAsync (KlijentId)
        // ============================================================
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> MojiOglasi(string filter = "Aktivni", string query = "")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Koristi novu metodu data-efcore-agenta: autor = KlijentId ILI RadnikId
            var oglasi = await _oglasService.GetOglasByAutorIdAsync(userId);

            if (!string.IsNullOrEmpty(query))
            {
                oglasi = oglasi.Where(o => o.Naslov != null && o.Naslov.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var sviOriginal = oglasi.ToList();
            var aktivniOriginal = sviOriginal.Where(o => o.Status != Status.Zavrsen && o.Status != Status.Neaktivan).ToList();
            var zavrseniOriginal = sviOriginal.Where(o => o.Status == Status.Zavrsen).ToList();

            if (filter == "Aktivni")
            {
                oglasi = aktivniOriginal;
            }
            else if (filter == "Zavrseni")
            {
                oglasi = zavrseniOriginal;
            }
            else
            {
                oglasi = sviOriginal;
            }

            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentQuery = query;
            ViewBag.UkupnoSvih = sviOriginal.Count;
            ViewBag.UkupnoAktivnih = aktivniOriginal.Count;
            ViewBag.UkupnoZavrsenih = zavrseniOriginal.Count;

            // Za Klijente koristi postojeći view (on sada radi univerzalno - "Moji oglasi" naslov)
            if (User.IsInRole(RoleConstants.Klijent) || User.IsInRole(RoleConstants.Admin))
            {
                return View("OglasiKlijenta", oglasi);
            }
            // Za Radnike koristi isti view (neutralni naslov "Moji oglasi")
            return View("OglasiKlijenta", oglasi);
        }

        [Authorize]
        public async Task<IActionResult> PrikazOglasa(string search, string lokacija, string tipPosla, string sort, int? minCijena, int? maxCijena)
        {
            if (minCijena.HasValue && (minCijena < 0 || minCijena > 9999999999999))
                ModelState.AddModelError("minCijena", "Minimalna cijena mora biti između 0 i 9999999999999.");

            if (maxCijena.HasValue && (maxCijena < 0 || maxCijena > 9999999999999))
                ModelState.AddModelError("maxCijena", "Maksimalna cijena mora biti između 0 i 9999999999999.");


            var oglasi = await _oglasService.SearchOglasiAsync(search, lokacija, tipPosla, sort, minCijena, maxCijena);
            var korisnikId = _userManager.GetUserId(User);
            var prijavljeniOglasi = korisnikId != null
                ? await _oglasService.GetPrijavljeniOglasiAsync(korisnikId)
                : new List<int>();

            ViewBag.PrijavljeniOglasiId = prijavljeniOglasi;
            return View(oglasi);
        }
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> OglasiKlijenta(string filter = "Aktivni", string query = "")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var oglasi = await _oglasService.GetOglasByKlijentIdAsync(userId);
            
            if (!string.IsNullOrEmpty(query))
            {
                oglasi = oglasi.Where(o => o.Naslov != null && o.Naslov.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var sviOriginal = oglasi.ToList();
            var aktivniOriginal = sviOriginal.Where(o => o.Status != Status.Zavrsen && o.Status != Status.Neaktivan).ToList();
            var zavrseniOriginal = sviOriginal.Where(o => o.Status == Status.Zavrsen).ToList();

            if (filter == "Aktivni")
            {
                oglasi = aktivniOriginal;
            }
            else if (filter == "Zavrseni")
            {
                oglasi = zavrseniOriginal;
            }
            else
            {
                oglasi = sviOriginal;
            }

            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentQuery = query;
            ViewBag.UkupnoSvih = sviOriginal.Count;
            ViewBag.UkupnoAktivnih = aktivniOriginal.Count;
            ViewBag.UkupnoZavrsenih = zavrseniOriginal.Count;

            return View(oglasi);
        }
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> PrijavljeniRadnici(int oglasId)
        {
            var prijave = await _oglasService.GetApplicantsForOglasAsync(oglasId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (prijave.Count == 0)
            {
                var oglas = await _oglasService.GetOglasByIdAsync(oglasId);
                if (oglas == null)
                    return NotFound();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                bool jeVlasnik = oglas.KlijentId == userId || oglas.RadnikId == userId;
                if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
                    return Forbid();
            }

            var ratings = new Dictionary<string, double>();
            foreach (var prijava in prijave)
            {
                var radnikId = prijava.KorisnikId;
                var workerReviews = await _context.Recenzija.Where(r => r.RadnikId == radnikId).ToListAsync();
                if (workerReviews.Any())
                {
                    ratings[radnikId] = workerReviews.Average(r => r.Ocjena);
                }
                else
                {
                    ratings[radnikId] = 0;
                }
            }
            ViewBag.Ratings = ratings;

            return View(prijave);
        }
        [Authorize(Roles = RoleConstants.Radnik)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrijaviRadnikaNaOglas(int oglasId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                return isAjax ? Json(new { success = false, message = "Niste prijavljeni." }) : RedirectToAction("PrijavaGreska");
            }

            var result = await _oglasService.ApplyToOglasAsync(oglasId, userId);
            bool ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!result)
            {
                return ajax
                    ? Json(new { success = false, message = "Već ste prijavljeni na ovaj oglas ili prijava nije uspjela." })
                    : RedirectToAction("PrijavaGreska");
            }

            return ajax
                ? Json(new { success = true, message = "Uspješno ste se prijavili na oglas." })
                : RedirectToAction("UspjesnaPrijava");
        }

        [Authorize(Roles = RoleConstants.Radnik)]
        public async Task<IActionResult> PrijaviSe(int oglasId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("PrijavaGreska");
            }

            var result = await _oglasService.ApplyToOglasAsync(oglasId, userId);
            if (!result)
            {
                return RedirectToAction("PrijavaGreska");
            }

            return RedirectToAction("UspjesnaPrijava");
        }


        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prihvati(int id)
        {
            var prijava = await _context.OglasKorisnik.FindAsync(id);
            if (prijava?.OglasId == null)
                return NotFound();

            var oglas = await _oglasService.GetOglasByIdAsync(prijava.OglasId.Value);
            var userId = _userManager.GetUserId(User);
            bool jeVlasnik = oglas?.KlijentId == userId || oglas?.RadnikId == userId;
            if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
                return Forbid();

            var result = await _oglasService.AcceptApplicationAsync(id);
            if (!result)
                return NotFound();

            return RedirectToAction("PrijavljeniRadnici", new { oglasId = prijava?.OglasId });
        }

        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Odbij(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var prijava = await _context.OglasKorisnik.FindAsync(id);
            if (prijava?.OglasId != null)
            {
                var oglas = await _oglasService.GetOglasByIdAsync(prijava.OglasId.Value);
                bool jeVlasnik = oglas?.KlijentId == userId || oglas?.RadnikId == userId;
                if (!jeVlasnik && !User.IsInRole(RoleConstants.Admin))
                    return Forbid();
            }

            var result = await _oglasService.RejectApplicationAsync(id, userId);
            if (!result)
                return NotFound();

            return RedirectToAction("PrijavljeniRadnici", new { oglasId = prijava?.OglasId });
        }

        // InitiatePayment ostaje samo za Klijenta (placanje je onaj koji klijent inicijalizuje ka radniku)
        [Authorize(Roles = RoleConstants.Klijent)]
        public async Task<IActionResult> InitiatePayment(int oglasId, string radnikId)
        {
            var oglas = await _oglasService.GetOglasByIdAsync(oglasId);
            if (oglas == null)
            {
                return NotFound();
            }

            TempData["OglasId"] = oglasId;
            TempData["RadnikId"] = radnikId;

            decimal cijenaKm = Convert.ToDecimal(oglas.CijenaPosla);
            long amountInCents = (long)Math.Round(cijenaKm * 100);

            var checkoutUrl = $"/Identity/Payment/Checkout?amount={amountInCents}&amountKm={cijenaKm.ToString(System.Globalization.CultureInfo.InvariantCulture)}&productName={Uri.EscapeDataString($"Plaćanje za oglas: {oglas.Naslov}")}";

            return Redirect(checkoutUrl);
        }

        [AllowAnonymous]
        public IActionResult UspjesnaPrijava()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult PrijavaGreska()
        {
            return View();
        }

        [Authorize(Roles = RoleConstants.Admin)]
        public IActionResult KreirajPosao()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> KreirajPosao(AdminOglasView model)
        {
            if (ModelState.IsValid)
            {
                var klijent = await _userManager.FindByEmailAsync(model.KlijentEmail);
                if (klijent == null)
                {
                    ModelState.AddModelError("KlijentEmail", "Klijent s tim emailom ne postoji.");
                    return View(model);
                }

                var oglas = new Oglas
                {
                    Naslov = model.Naslov,
                    Opis = model.Opis,
                    Lokacija = model.Lokacija,
                    TipPosla = model.TipPosla,
                    CijenaPosla = model.CijenaPosla,
                    KlijentId = klijent.Id,
                    Status = Status.Aktivan
                };

                await _oglasService.CreateOglasAsync(oglas, klijent.Id, RoleConstants.Klijent);
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
        [Authorize(Roles = RoleConstants.Radnik)]
        public async Task<IActionResult> MojePrijave(string filter = "Sve", string query = "")
        {
            var radnikId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var prijave = await _oglasService.GetRadnikPrijaveAsync(radnikId);

            if (!string.IsNullOrEmpty(query))
            {
                prijave = prijave.Where(p => p.Oglas != null && p.Oglas.Naslov != null
                    && p.Oglas.Naslov.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (filter == "Aktivne")
            {
                prijave = prijave.Where(p => p.Status == Status.Aktivan || p.Status == Status.Prihvacen).ToList();
            }
            else if (filter == "Zavrsene")
            {
                prijave = prijave.Where(p => p.Status == Status.Zavrsen || p.Status == Status.Placen).ToList();
            }

            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentQuery = query;

            return View(prijave);
        }

        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent)]
        public async Task<IActionResult> ProfilRadnika(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var radnik = await _userManager.FindByIdAsync(id);
            if (radnik == null)
            {
                return NotFound();
            }

            var reviews = await _context.Recenzija.Where(r => r.RadnikId == id).ToListAsync();
            var rating = reviews.Any() ? reviews.Average(r => r.Ocjena) : 0;
            
            var clientIds = reviews.Select(r => r.KlijentId).Distinct().ToList();
            var clients = await _userManager.Users.Where(u => clientIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.Ime) ? u.Email : $"{u.Ime} {u.Prezime}");
            
            ViewBag.Radnik = radnik;
            ViewBag.ProsjecnaOcjena = rating;
            ViewBag.ClientNames = clients;
            
            return View(reviews);
        }
    }
}
