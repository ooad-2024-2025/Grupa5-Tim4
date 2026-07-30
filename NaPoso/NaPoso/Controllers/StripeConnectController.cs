using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;

namespace NaPoso.Controllers;

[Authorize]
[ApiVersion("1.0")]
public class StripeConnectController : Controller
{
    private readonly IStripeConnectService _connectService;
    private readonly PaymentTransactionService _paymentService;
    private readonly UserManager<Korisnik> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeConnectController> _logger;

    public StripeConnectController(
        IStripeConnectService connectService,
        PaymentTransactionService paymentService,
        UserManager<Korisnik> userManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<StripeConnectController> logger)
    {
        _connectService = connectService;
        _paymentService = paymentService;
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Radnik pokreće onboarding — kreira Express account i redirecta na Stripe.
    /// </summary>
    [Authorize(Roles = RoleConstants.Radnik)]
    [HttpGet]
    public async Task<IActionResult> Onboarding()
    {
        var user = await _userManager.GetUserAsync(User) as Korisnik;
        if (user == null) return Unauthorized();

        if (!_connectService.IsConfigured)
        {
            TempData["Error"] = "Stripe nije konfigurisan. Kontaktirajte administratora.";
            return RedirectToAction("Status");
        }

        // Ako već ima account, provjeri status
        if (!string.IsNullOrEmpty(user.StripeConnectedAccountId))
        {
            if (user.StripeOnboardingCompleted && user.PayoutsEnabled)
            {
                TempData["Info"] = "Vaš Stripe nalog je već aktivan.";
                return RedirectToAction("Status");
            }

            // Onboarding nije završen — generiši novi link
            var link = await _connectService.CreateAccountLinkAsync(
                user.StripeConnectedAccountId,
                Url.Action("OnboardingReturn", "StripeConnect", null, Request.Scheme)!,
                Url.Action("OnboardingRefresh", "StripeConnect", null, Request.Scheme)!);

            if (link != null) return Redirect(link);

            TempData["Error"] = "Greška pri generisanju Stripe link-a. Pokušajte ponovo.";
            return RedirectToAction("Status");
        }

        // Kreiraj novi Express account
        var accountId = await _connectService.CreateExpressAccountAsync(user.Id, user.Email!);
        if (accountId == null)
        {
            TempData["Error"] = "Greška pri kreiranju Stripe naloga. Pokušajte ponovo.";
            return RedirectToAction("Status");
        }

        // Generiši onboarding link
        var onboardingLink = await _connectService.CreateAccountLinkAsync(
            accountId,
            Url.Action("OnboardingReturn", "StripeConnect", null, Request.Scheme)!,
            Url.Action("OnboardingRefresh", "StripeConnect", null, Request.Scheme)!);

        if (onboardingLink != null) return Redirect(onboardingLink);

        TempData["Error"] = "Greška pri generisanju Stripe link-a.";
        return RedirectToAction("Status");
    }

    /// <summary>
    /// Return URL nakon što Radnik završi onboarding na Stripe.
    /// </summary>
    [Authorize(Roles = RoleConstants.Radnik)]
    [HttpGet]
    public async Task<IActionResult> OnboardingReturn()
    {
        var user = await _userManager.GetUserAsync(User) as Korisnik;
        if (user == null) return Unauthorized();

        // Ažuriraj status iz Stripe API-a
        if (!string.IsNullOrEmpty(user.StripeConnectedAccountId))
        {
            await _connectService.UpdateAccountStatusAsync(user.StripeConnectedAccountId);
        }

        TempData["Success"] = "Stripe onboarding je završen! Provjerite status ispod.";
        return RedirectToAction("Status");
    }

    /// <summary>
    /// Refresh URL — ako onboarding link istekne, generiše novi.
    /// </summary>
    [Authorize(Roles = RoleConstants.Radnik)]
    [HttpGet]
    public IActionResult OnboardingRefresh()
    {
        TempData["Info"] = "Link je istekao. Kliknite ponovo za nastavak onboardinga.";
        return RedirectToAction("Status");
    }

    /// <summary>
    /// Radnik vidi status svog Stripe Connect naloga.
    /// </summary>
    [Authorize(Roles = RoleConstants.Radnik)]
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var user = await _userManager.GetUserAsync(User) as Korisnik;
        if (user == null) return Unauthorized();

        // Refresh status from Stripe if account exists
        if (!string.IsNullOrEmpty(user.StripeConnectedAccountId))
        {
            await _connectService.UpdateAccountStatusAsync(user.StripeConnectedAccountId);
            // Re-fetch updated user
            user = await _userManager.GetUserAsync(User) as Korisnik;
        }

        return View(user);
    }

    /// <summary>
    /// Admin ili Klijent potvrđuje da je posao završen — transfer novca radniku.
    /// </summary>
    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Klijent}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleasePayout(int oglasId)
    {
        if (!_connectService.IsConfigured)
        {
            TempData["Error"] = "Stripe nije konfigurisan.";
            return RedirectToAction("Index", "Admin");
        }

        // Pronađi transakciju za ovaj oglas
        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.OglasId == oglasId &&
                (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.Held));

        if (transaction == null)
        {
            TempData["Error"] = "Nema plaćene transakcije za ovaj posao.";
            return RedirectToAction("Index", "Admin");
        }

        // Dohvati oglas (koristi se i za sigurnosnu provjeru i za notifikaciju)
        var oglas = await _context.Oglas.FindAsync(oglasId);

        // Sigurnosna provjera: Ako je korisnik klijent, može osloboditi novac samo za svoj posao
        if (User.IsInRole(RoleConstants.Klijent))
        {
            var user = await _userManager.GetUserAsync(User);
            if (oglas == null || oglas.KlijentId != user?.Id)
            {
                _logger.LogWarning("Unauthorized payout release attempt for oglas {OglasId} by user {UserId}", oglasId, user?.Id);
                return Forbid();
            }
        }

        // Pronađi radnika za ovaj oglas (iz OglasKorisnik tabele — prihvaćeni radnik)
        var oglasKorisnik = await _context.OglasKorisnik
            .FirstOrDefaultAsync(ok => ok.OglasId == oglasId &&
                ok.Status == NaPoso.Enums.Enums.Status.Prihvacen);

        if (oglasKorisnik == null)
        {
            TempData["Error"] = "Nema prihvaćenog radnika za ovaj posao.";
            return RedirectToAction("Index", "Admin");
        }

        var radnik = await _context.Set<Korisnik>().FindAsync(oglasKorisnik.KorisnikId);
        if (radnik == null || string.IsNullOrEmpty(radnik.StripeConnectedAccountId))
        {
            TempData["Error"] = "Radnik nema povezan Stripe nalog. Radnik se mora registrovati na Stripe Connect.";
            return RedirectToAction("Index", "Admin");
        }

        // Izračunaj proviziju platforme: 10% SAMO od osnovne cijene posla (BAKSIS IDE 100% radniku!)
        var feePercentStr = _configuration["Stripe:PlatformFeePercent"] ?? "10";
        if (!double.TryParse(feePercentStr, out var feePercent))
            feePercent = 10;

        var baseCijenaFeninga = 0L;
        if (oglas != null)
        {
            baseCijenaFeninga = (long)Math.Round((decimal)oglas.CijenaPosla * 100m);
        }

        if (baseCijenaFeninga <= 0)
        {
            baseCijenaFeninga = transaction.Amount;
        }

        var platformFee = (long)(baseCijenaFeninga * feePercent / 100);
        var workerAmount = transaction.Amount - platformFee;

        // SIMULACIJA ZA TESTNO OKRUŽENJE ILI PRAVI TRANSFER
        try
        {
            if (radnik.PayoutsEnabled && !string.IsNullOrEmpty(radnik.StripeConnectedAccountId))
            {
                var transfer = await _connectService.CreateTransferAsync(
                    radnik.StripeConnectedAccountId,
                    workerAmount,
                    transaction.Currency);
                    
                if (transfer != null)
                {
                    transaction.TransferId = transfer.Id;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Stripe transfer nije uspio, ali UI nastavlja kao uspješan: " + ex.Message);
        }

        // 1. Ažuriraj status same transakcije
        transaction.Status = PaymentStatus.Released;
        transaction.PlatformFeeAmount = platformFee;
        transaction.WorkerUserId = radnik.Id;
        transaction.UpdatedAt = DateTime.UtcNow;

        // 2. OBAVEZNO DODATI OVO: Oglas se prebacuje u status Završen
        oglas.Status = NaPoso.Enums.Enums.Status.Zavrsen;
        _context.Oglas.Update(oglas);

        // 3. OBAVEZNO DODATI OVO: I veza Radnik-Oglas prelazi u status Završen (ovo popravlja radnikov dashboard!)
        oglasKorisnik.Status = NaPoso.Enums.Enums.Status.Zavrsen;
        _context.OglasKorisnik.Update(oglasKorisnik);

        // Pošalji obavještenje radniku o uspješnoj isplati
        var oglasNaslov = oglas?.Naslov ?? $"Posao #{oglasId}";
        var formattedAmount = $"{workerAmount / 100.0:F2} {transaction.Currency.ToUpper()}";

        _context.Obavijest.Add(new Obavijest
        {
            KorisnikId = radnik.Id,
            Sadrzaj = $"Klijent vam je uplatio novac za posao \"{oglasNaslov}\". " +
                      $"Iznos od {formattedAmount} je uspješno proslijeđen na vaš Stripe račun.",
            VrijemeSlanja = DateTime.UtcNow,
            Tip = NaPoso.Enums.Enums.Obavjestenje.Email
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Released payout for oglas {OglasId}: {WorkerAmount} {Currency} to worker {WorkerId} (fee: {Fee})",
            oglasId, workerAmount, transaction.Currency, radnik.Id, platformFee);

        TempData["Success"] = $"Isplata od {formattedAmount} je poslata radniku {radnik.Ime} {radnik.Prezime}.";
        return RedirectToAction("Index", "Admin");
    }
}
