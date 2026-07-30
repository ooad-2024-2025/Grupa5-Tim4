using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NaPoso.Constants;
using NaPoso.Models;
using NaPoso.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NaPoso.Services; // IOglasService

namespace NaPoso.Controllers
{
    [ApiVersion("1.0")]
    public class HomeController : Controller
    {
        private readonly UserManager<Korisnik> _userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IOglasService _oglasService;

        public HomeController(UserManager<Korisnik> userManager, ILogger<HomeController> logger, ApplicationDbContext context, IOglasService oglasService)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _oglasService = oglasService;
        }
            public async Task<IActionResult> Index()
            {
                /*if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Contains("Radnik"))
                        return RedirectToAction("Radnik");
                    else if (roles.Contains("Klijent"))
                        return RedirectToAction("Klijent");
            }
            */

                return View(); // ako nije logovan, poka�i home stranicu
            }
        [Authorize(Roles = RoleConstants.Admin)]
        public IActionResult Admin()
        {
            return View();
        }
        [Authorize(Roles = RoleConstants.Radnik)]
        public async Task<IActionResult> Radnik()
        {
            var radnikId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var prijavljeni = await _context.OglasKorisnik.CountAsync(ok => ok.KorisnikId == radnikId);
            var zavrseni = await _context.OglasKorisnik.CountAsync(ok =>
                ok.KorisnikId == radnikId &&
                (ok.Status == Enums.Enums.Status.Zavrsen || ok.Status == Enums.Enums.Status.Placen));

            // ============================================================
            // IZVOR ISTINE ZA ZARADU (BAKSIS IDE 100% RADNIKU):
            // 1. PaymentTransactions: WorkerUserId = radnik, Status = Released/Paid/Held
            //    Radnik dobija: (Amount - PlatformFeeAmount)
            //    - Amount = UKUPAN iznos (osnova + baksis)
            //    - PlatformFeeAmount = 10% SAMO od osnovne cijene (bez baksisa)
            //    → Dakle: Amount - PlatformFeeAmount = (osnova - 10% osnove) + 100% baksis
            // 2. Fallback: stari OglasKorisnik (zavrseni/placeni) bez transakcije
            //    → 90% CijenaPosla (osnova) - pokušavamo i naći baksis
            // ============================================================
            var transakcije = await _context.PaymentTransactions
                .Where(pt => pt.WorkerUserId == radnikId &&
                             (pt.Status == PaymentStatus.Released ||
                              pt.Status == PaymentStatus.Paid ||
                              pt.Status == PaymentStatus.Held))
                .ToListAsync();

            var zaradaIzvrseneIsplate = 0m;
            decimal ukupniBaksis = 0m;
            var povezaniOglasiIds = new HashSet<int>();

            foreach (var pt in transakcije)
            {
                decimal zaradaZaOvuTransakciju;
                if (pt.PlatformFeeAmount.HasValue)
                {
                    // Formula: Amount (osnova + baksis) - 10% provizije (samo od osnove)
                    // → Ovdje je sigurno uračunat i baksis 100% u korist radnika
                    zaradaZaOvuTransakciju = (decimal)(pt.Amount - pt.PlatformFeeAmount.Value) / 100m;
                }
                else
                {
                    // Ako nema PlatformFee, pretpostavljamo 10% provizije od cijelog Amount
                    zaradaZaOvuTransakciju = (decimal)pt.Amount * 0.90m / 100m;
                }
                zaradaIzvrseneIsplate += zaradaZaOvuTransakciju;

                // Poveži sa oglasom da ne bi uračunali fallback za njega
                if (pt.OglasId.HasValue)
                {
                    povezaniOglasiIds.Add(pt.OglasId.Value);

                    // EKSPLICITNO izračunaj baksis:
                    // Baksis = Transaction.Amount - (oglas.CijenaPosla * 100)
                    // Ako je Amount > CijenaPosla*100, razlika je BAKŠIŠ 100% za radnika
                    // VAŽNO: za financijski look-up ignoriramo soft-delete filter (zavrseni/obrisani
                    //   oglas je deo istorije, mora biti dostupan za izračun).
                    var oglas = await _context.Oglas
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(o => o.Id == pt.OglasId.Value);
                    if (oglas != null)
                    {
                        var osnovaFeninga = (long)Math.Round((decimal)oglas.CijenaPosla * 100m);
                        var baksisFeninga = pt.Amount - osnovaFeninga;
                        if (baksisFeninga > 0)
                        {
                            ukupniBaksis += (decimal)baksisFeninga / 100m;
                        }
                    }
                }
            }

            var zavrseniOglasiIds = await _context.OglasKorisnik
                .Where(ok => ok.KorisnikId == radnikId &&
                             (ok.Status == Enums.Enums.Status.Zavrsen || ok.Status == Enums.Enums.Status.Placen) &&
                             ok.OglasId.HasValue)
                .Select(ok => ok.OglasId.Value)
                .ToListAsync();

            var nedostajuciOglasiIds = zavrseniOglasiIds.Except(povezaniOglasiIds).ToList();

            var zaradaFallback = 0m;
            if (nedostajuciOglasiIds.Any())
            {
                zaradaFallback = await _context.Oglas
                    .Where(o => nedostajuciOglasiIds.Contains(o.Id))
                    .SumAsync(o => (decimal)o.CijenaPosla * 0.90m);
            }

            var ukupnoZaradjeno = zaradaIzvrseneIsplate + zaradaFallback;

            // --- GODIŠNJA ZARADA ZA GRAFIKON (po mjesecima, tekuća godina) ---
            var zaradaPoMjesecima = new Dictionary<string, double>();
            var tekućaGodina = DateTime.UtcNow.Year;
            for (int m = 1; m <= 12; m++)
            {
                zaradaPoMjesecima[m.ToString("00")] = 0;
            }

            // Glavna transakcijska petlja — format ključa: Month.ToString("00")
            foreach (var pt in transakcije)
            {
                var datum = pt.UpdatedAt ?? pt.PaidAt ?? pt.CreatedAt;
                if (datum.Year == tekućaGodina)
                {
                    decimal zaradaZaOvu;
                    if (pt.PlatformFeeAmount.HasValue)
                        zaradaZaOvu = (decimal)(pt.Amount - pt.PlatformFeeAmount.Value) / 100m;
                    else
                        zaradaZaOvu = (decimal)pt.Amount * 0.90m / 100m;

                    // ISTI format ključa kao inicijalizacija (Month Dvodigit)
                    var mjesecKljuč = datum.Month.ToString("00");
                    zaradaPoMjesecima[mjesecKljuč] += Math.Round((double)zaradaZaOvu, 2);
                }
            }

            // Fallback za nedostajuće oglase — ISTI mjesec format ključa
            if (nedostajuciOglasiIds.Any())
            {
                var nedostajućiOglasi = await _context.OglasKorisnik
                    .Include(ok => ok.Oglas)
                    .Where(ok => ok.KorisnikId == radnikId &&
                                 ok.OglasId.HasValue &&
                                 nedostajuciOglasiIds.Contains(ok.OglasId.Value))
                    .ToListAsync();

                foreach (var ok in nedostajućiOglasi)
                {
                    var datum = ok.DatumPrijave;
                    if (ok.Oglas != null && datum.Year == tekućaGodina)
                    {
                        var zarada = (decimal)ok.Oglas.CijenaPosla * 0.90m;
                        // ISTI format: Month.ToString("00")
                        var mjesecKljuč = datum.Month.ToString("00");
                        zaradaPoMjesecima[mjesecKljuč] += Math.Round((double)zarada, 2);
                    }
                }
            }

            var godisnjaUkupno = Math.Round(zaradaPoMjesecima.Values.Sum(), 2);
            var prosjecnaMjesecna = godisnjaUkupno > 0 ? (decimal)Math.Round(godisnjaUkupno / 12, 2) : 0m;

            ViewBag.Prijavljeni = prijavljeni;
            ViewBag.Zavrseni = zavrseni;

            // SVE VRIJEME (sve transakcije + fallback, bez obzira na godinu)
            ViewBag.UkupnoZaradjeno = Math.Round(ukupnoZaradjeno, 2);
            ViewBag.UkupnaZaradaSveVrijeme = Math.Round(ukupnoZaradjeno, 2);
            ViewBag.UkupniBaksis = Math.Round(ukupniBaksis, 2);
            // Osnovica = Ukupna zarada (sad već uključuje bakšiš) - Bakšiš = isključivo osnovni dio
            //   (konzistentno sa prikazom na UI-u: Osnovica + Bakšiš = Ukupno)
            ViewBag.OsnovicaZarada = Math.Round(ukupnoZaradjeno - ukupniBaksis, 2);

            // TEKUĆA GODINA (po mjesecima za chart i kartice)
            ViewBag.GodišnjaZaradaJson = System.Text.Json.JsonSerializer.Serialize(zaradaPoMjesecima.Values.ToList());
            ViewBag.GodišnjaZaradaUkupno = godisnjaUkupno;
            ViewBag.ProsjecnaMjesecnaZarada = prosjecnaMjesecna;

            return View();
        }

        [Authorize(Roles = RoleConstants.Klijent)]
        public async Task<IActionResult> Klijent()
        {
            var klijentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var aktivniOglasi = await _context.Oglas.CountAsync(o => o.KlijentId == klijentId && o.Status == NaPoso.Enums.Enums.Status.Aktivan);
            var zavrseniOglasi = await _context.Oglas.CountAsync(o => o.KlijentId == klijentId && o.Status == NaPoso.Enums.Enums.Status.Zavrsen);

            // Ukupno plaćeno = osnovica + bakšiš iz stvarnih transakcija
            //   pt.Amount (fening) = osnova (CijenaPosla*100) + baksis (fening)
            //   → (decimal)pt.Amount / 100m = tačan ukupni iznos koji je klijent potrošio
            var transakcije = await _context.PaymentTransactions
                .Where(pt => pt.UserId == klijentId &&
                             (pt.Status == PaymentStatus.Released ||
                              pt.Status == PaymentStatus.Paid ||
                              pt.Status == PaymentStatus.Held))
                .ToListAsync();

            decimal ukupnoPotroseno = 0m;
            decimal ukupniBaksis = 0m;
            var placeniOglasiIds = new HashSet<int>();

            foreach (var pt in transakcije)
            {
                ukupnoPotroseno += (decimal)pt.Amount / 100m;

                if (pt.OglasId.HasValue)
                {
                    placeniOglasiIds.Add(pt.OglasId.Value);
                    // VAŽNO: za finansijski look-up ignoriramo soft-delete filter
                    //   (istorijska transakcija mora imati pristup oglasu bez obzira na status)
                    var oglas = await _context.Oglas
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(o => o.Id == pt.OglasId.Value);
                    if (oglas != null)
                    {
                        // Osnova = CijenaPosla u KM, baksis = preostali dio Amounta
                        var osnovaFeninga = (long)Math.Round((decimal)oglas.CijenaPosla * 100m);
                        var baksisFeninga = pt.Amount - osnovaFeninga;
                        if (baksisFeninga > 0)
                            ukupniBaksis += (decimal)baksisFeninga / 100m;
                    }
                }
            }

            // --- GODIŠNJA POTROŠNJA ZA GRAFIKON (po mjesecima, tekuća godina) ---
            var potrošnjaPoMjesecima = new Dictionary<string, double>();
            var tekućaGodina = DateTime.UtcNow.Year;
            for (int m = 1; m <= 12; m++)
            {
                potrošnjaPoMjesecima[m.ToString("00")] = 0;
            }

            // 1) Transakcije u tekućoj godini
            foreach (var pt in transakcije)
            {
                var datum = pt.UpdatedAt ?? pt.PaidAt ?? pt.CreatedAt;
                if (datum.Year == tekućaGodina)
                {
                    var iznosKM = (decimal)pt.Amount / 100m;
                    var mjesecKljuč = datum.Month.ToString("00");
                    potrošnjaPoMjesecima[mjesecKljuč] += Math.Round((double)iznosKM, 2);
                }
            }

            // 2) Fallback za završene oglase bez transakcije
            var zavrseniBezTransakcije = await _context.Oglas
                .Where(o => o.KlijentId == klijentId &&
                            o.Status == NaPoso.Enums.Enums.Status.Zavrsen &&
                            !placeniOglasiIds.Contains(o.Id))
                .ToListAsync();

            decimal fallbackUkupno = zavrseniBezTransakcije.Sum(o => (decimal)o.CijenaPosla);
            ukupnoPotroseno += fallbackUkupno;

            // 3) Fallback u godišnju potrošnju — koristi DatumPrijave (kada je klijent
            //    najvjerovatnije izvršio transakciju) kao datum za mjesec.
            if (zavrseniBezTransakcije.Any())
            {
                var oglasiSaPrijavom = await _context.OglasKorisnik
                    .Include(ok => ok.Oglas)
                    .Where(ok => ok.OglasId.HasValue &&
                                 ok.Oglas != null &&
                                 ok.Oglas.KlijentId == klijentId &&
                                 ok.Oglas.Status == NaPoso.Enums.Enums.Status.Zavrsen &&
                                 !placeniOglasiIds.Contains(ok.Oglas.Id))
                    .GroupBy(ok => ok.OglasId!.Value)
                    .Select(g => g.OrderByDescending(ok => ok.DatumPrijave).First())
                    .ToListAsync();

                foreach (var ok in oglasiSaPrijavom)
                {
                    if (ok.DatumPrijave.Year == tekućaGodina && ok.Oglas != null)
                    {
                        var mjesecKljuč = ok.DatumPrijave.Month.ToString("00");
                        potrošnjaPoMjesecima[mjesecKljuč] += Math.Round((double)ok.Oglas.CijenaPosla, 2);
                    }
                }
            }

            var godišnjaPotrošnjaUkupno = Math.Round(potrošnjaPoMjesecima.Values.Sum(), 2);
            var prosječnaMjesečnaPotrošnja = godišnjaPotrošnjaUkupno > 0
                ? (decimal)Math.Round(godišnjaPotrošnjaUkupno / 12, 2)
                : 0m;

            ViewBag.AktivniOglasi = aktivniOglasi;
            ViewBag.ZavrseniOglasi = zavrseniOglasi;

            // SVE VRIJEME (transakcije + fallback)
            ViewBag.UkupnoPotroseno = Math.Round(ukupnoPotroseno, 2);
            ViewBag.UkupnaPotrošnjaSveVrijeme = Math.Round(ukupnoPotroseno, 2);
            ViewBag.UkupniBaksis = Math.Round(ukupniBaksis, 2);
            ViewBag.OsnovicaPotrosena = Math.Round(ukupnoPotroseno - ukupniBaksis, 2);

            // TEKUĆA GODINA (po mjesecima, za chart i kartice)
            ViewBag.GodišnjaPotrošnjaPoMjesecima = potrošnjaPoMjesecima;
            ViewBag.GodišnjaPotrošnjaUkupno = godišnjaPotrošnjaUkupno;
            ViewBag.ProsječnaMjesečnaPotrošnja = prosječnaMjesečnaPotrošnja;
            ViewBag.GodišnjaPotrošnjaJson = System.Text.Json.JsonSerializer.Serialize(potrošnjaPoMjesecima.Values.ToList());

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ============================================================
        // DEBUG ENDPOINT za hvatanje PRAVOG exceptiona koji se baci
        // kada korisnik (klijent/radnik) posjeti Oglas akcije.
        // Ovdje simuliramo EXAKT isti kod kao u OglasControlleru,
        // samo bez [Authorize] atributa, za potrebe debugiranja.
        // ============================================================
        [Route("/debug-oglasi")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DebugOglasAkcije()
        {
            try
            {
                // --- 1. Simuliraj OglasiKlijenta ---
                _logger.LogInformation("[DEBUG] Test 1: GetOglasByKlijentIdAsync (testiramo sa null user)...");
                var testUserId = (await _context.Users.OrderBy(u => u.Id).Select(u => u.Id).FirstOrDefaultAsync()) ?? "test";
                var oglasiKlijenta = await _oglasService.GetOglasByKlijentIdAsync(testUserId);
                _logger.LogInformation("[DEBUG] Test 1 USPJEH: vraceno {Count} oglasa", oglasiKlijenta.Count);

                // --- 2. Simuliraj PrikazOglasa (SearchOglasiAsync) ---
                _logger.LogInformation("[DEBUG] Test 2: SearchOglasiAsync (svi defaulti)...");
                var pretraga = await _oglasService.SearchOglasiAsync(null, null, null, null, null, null);
                _logger.LogInformation("[DEBUG] Test 2 USPJEH: vraceno {Count} oglasa", pretraga.Count);

                // --- 3. Simuliraj GetPrijavljeniOglasi ---
                _logger.LogInformation("[DEBUG] Test 3: GetPrijavljeniOglasiAsync...");
                var prijavljeni = await _oglasService.GetPrijavljeniOglasiAsync(testUserId);
                _logger.LogInformation("[DEBUG] Test 3 USPJEH: {Count} prijava", prijavljeni.Count);

                // --- 4. Test StatisticsService (ukoliko je njega krivo odradio) ---
                _logger.LogInformation("[DEBUG] Test 4: StatisticsService.GetStatisticsAsync...");
                var statisticsService = HttpContext.RequestServices.GetRequiredService<IStatisticsService>();
                var stats = await statisticsService.GetStatisticsAsync();
                _logger.LogInformation("[DEBUG] Test 4 USPJEH: poslova={Poslova}, aktivnih={Aktivnih}, zavrsenih={Zavrsenih}",
                    stats.BrojPoslova, stats.AktivniPoslovi, stats.BrojZavrsenihPoslova);

                // --- 5. Simuliraj Home.Radnik akciju (ona koja mijenja sve) ---
                _logger.LogInformation("[DEBUG] Test 5: HomeController.Radnik (transakcije i grafikon)...");
                var testRadnikId = testUserId;
                var transakcije = await _context.PaymentTransactions
                    .Where(pt => pt.WorkerUserId == testRadnikId)
                    .ToListAsync();
                _logger.LogInformation("[DEBUG] Test 5.1 USPJEH: transakcija {Count}", transakcije.Count);

                var sviOglasiDb = await _context.Oglas.ToListAsync();
                _logger.LogInformation("[DEBUG] Test 5.2 USPJEH: svi Oglas={Count}, (migracija IsDeleted postoji, prva 3: {Prvi3})",
                    sviOglasiDb.Count,
                    string.Join(",", sviOglasiDb.Take(3).Select(o => $"{o.Id}:IsDel={o.IsDeleted}")));

                return Content("OK - svi testovi prosli. Ispisi u konzoli servera za detalje.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEBUG] EXCEPTION uhvacen: {Message}", ex.Message);
                return Content($"EXCEPTION: {ex.GetType().Name}\nMessage: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}\n\n---Inner---\n{ex.InnerException?.ToString() ?? "(null)"}");
            }
        }
    }
}
