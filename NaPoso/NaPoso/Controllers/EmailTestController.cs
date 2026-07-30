using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using NaPoso.Constants;
using NaPoso.Models;
using NaPoso.Services;

namespace NaPoso.Controllers
{
    /// <summary>
    /// Test / dijelovi API za provjeru Brevo email integracije.
    /// Endpointi:
    ///   GET /api/email/config  → javno (anonimno) da li su config vrijednosti postavljene (ne izlaže tajne)
    ///   GET /api/email/test    → [Authorize(Roles = Admin)] šalje test email i vraća detalje slanja
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/email")]
    [ApiController]
    [Produces("application/json")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly UserManager<Korisnik> _userManager;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(
            IEmailService emailService,
            IConfiguration configuration,
            UserManager<Korisnik> userManager,
            ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Javni endpoint — da li su Brevo config postavke prisutne (ne izlaže API ključ, samo da/ne za svaku).
        /// Anonimno radi za provjeru bez prijave.
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public IActionResult GetConfigStatus()
        {
            static bool HasValue(string? v) => !string.IsNullOrWhiteSpace(v) &&
                                         !v!.Contains("BREVO_API_KEY", StringComparison.Ordinal) &&
                                         v != "<ovdje ide API ključ sa Brevo dashboard-a>" &&
                                         v != "noreply@naposo.example.com";

            string apiKey =
                Environment.GetEnvironmentVariable("BREVO_API_KEY")
                ?? Environment.GetEnvironmentVariable("EMAIL_BREVO_API_KEY")
                ?? _configuration["Email:Brevo:ApiKey"]
                ?? "";
            string senderEmail =
                Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL")
                ?? Environment.GetEnvironmentVariable("EMAIL_FROM")
                ?? _configuration["Email:SenderEmail"]
                ?? _configuration["Email:From"]
                ?? "";
            string senderName =
                Environment.GetEnvironmentVariable("BREVO_SENDER_NAME")
                ?? _configuration["Email:SenderName"]
                ?? _configuration["Email:Brevo:SenderName"]
                ?? "";
            string baseUrl =
                Environment.GetEnvironmentVariable("BREVO_BASE_URL")
                ?? Environment.GetEnvironmentVariable("EMAIL_BREVO_BASE_URL")
                ?? _configuration["Email:Brevo:BaseUrl"]
                ?? "";

            bool configOk = HasValue(apiKey) && HasValue(senderEmail);
            return base.Ok(new
            {
                brevoApiKeyConfigured = HasValue(apiKey),
                brevoSenderEmailConfigured = HasValue(senderEmail),
                brevoSenderNameConfigured = !string.IsNullOrWhiteSpace(senderName),
                brevoBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.brevo.com/v3 (default)" : baseUrl,
                configured = configOk,
                hint = configOk
                    ? "Sve postavke su prisutne. Idi na /api/email/test?to=tvoj@email.com da pokreneš slanje test emaila (potrebno biti ulogovan kao Admin)."
                    : "Postavi BREVO_API_KEY + BREVO_SENDER_EMAIL u .env fajlu (root projekta). Za BREVO_SENDER_EMAIL obavezno dodaj i verifikuj sender na https://app.brevo.com/settings/senders."
            });
        }

        /// <summary>
        /// Admin-only endpoint — šalje test email putem IEmailService (BrevoEmailService → Brevo REST API).
        /// </summary>
        /// <param name="to">Email adresa primaoca (obavezno). Npr. ?to=tvoj@email.ba</param>
        /// <param name="name">Ime primaoca (opciono). Npr. &amp;name=Meho Mehić</param>
        [HttpGet("test")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> SendTestEmail(
            [FromQuery(Name = "to")] string? to = null,
            [FromQuery(Name = "name")] string? name = null)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                // Ako korisnik nije prosljedio to, pokušaj uzeti trenutnog korisnika ili ADMIN_EMAIL iz ENV
                var curr = await _userManager.GetUserAsync(User);
                if (curr != null && !string.IsNullOrWhiteSpace(curr.Email))
                    to = curr.Email;
                else
                    to = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@mail.com";
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                var curr = await _userManager.GetUserAsync(User);
                name = curr?.Ime ?? curr?.Prezime ?? "Admin NaPos'o";
            }

            string subject = $"🧪 Test email s NaPos'o Brevo integracije ({DateTime.Now:HH:mm:ss})";
            string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0F1416; color: #FFFFFF; padding: 24px; }}
        .wrap {{ max-width: 620px; margin: 0 auto; background: #0E3B36; border-radius: 16px; padding: 32px; box-shadow: 0 8px 20px rgba(0,0,0,0.25); }}
        h1 {{ color: #FF3D68; margin: 0 0 16px 0; font-size: 28px; }}
        .pill {{ display: inline-block; background: rgba(0,224,184,0.18); color: #00E0B8; border-radius: 999px; padding: 4px 14px; font-size: 13px; font-weight: 600; margin-bottom: 20px; }}
        .row {{ padding: 10px 0; border-bottom: 1px dashed rgba(255,255,255,0.1); font-size: 15px; }}
        .row b {{ color: #FF7A96; min-width: 160px; display: inline-block; }}
        .footer {{ margin-top: 32px; text-align: center; font-size: 12px; color: #8FA4A2; padding-top: 16px; border-top: 1px solid rgba(255,255,255,0.1); }}
        .ok {{ color: #00E0B8; font-weight: 700; }}
    </style>
</head>
<body>
<div class='wrap'>
    <h1>🧪 Test email — Brevo integracija</h1>
    <span class='pill'>Uspješna integracija ✅</span>

    <div class='row'><b>Vrijeme slanja:</b> {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}</div>
    <div class='row'><b>Primaoc:</b> {System.Net.WebUtility.HtmlEncode(to)}</div>
    <div class='row'><b>Ime primaoca:</b> {System.Net.WebUtility.HtmlEncode(name)}</div>
    <div class='row'><b>Sender (iz konfiga):</b> <span class='ok'>BREVO_SENDER_EMAIL + BREVO_SENDER_NAME</span></div>
    <div class='row'><b>Protok:</b> HTTPS POST → https://api.brevo.com/v3/smtp/email</div>
    <div class='row'><b>Auth header:</b> api-key (BREVO_API_KEY iz .env)</div>

    <div style='margin-top: 24px; padding: 16px; background: rgba(255,255,255,0.05); border-radius: 10px; font-size: 14px; line-height: 1.55;'>
        💡 Ako si dobio/la ovaj mail u inboxu, znači da cijeli lanac radi:<br/>
        <b>.env konfig</b> → <b>IEmailService</b> → <b>BrevoEmailService (HttpClient)</b> → <b>Brevo REST API</b> → <b>tvoj mail server</b> → <b>inbox</b>.
    </div>

    <div class='footer'>
        Automatski generisan test email od strane NaPos'o platforme.<br/>
        &copy; {DateTime.Now.Year} NaPos'o. Sva prava zadržana.
    </div>
</div>
</body>
</html>";

            var sw = Stopwatch.StartNew();
            try
            {
                await _emailService.SendEmailAsync(toEmail: to, toName: name, subject: subject, htmlContent: htmlContent);
                sw.Stop();

                var senderEmail =
                    Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL")
                    ?? Environment.GetEnvironmentVariable("EMAIL_FROM")
                    ?? _configuration["Email:SenderEmail"]
                    ?? _configuration["Email:From"]
                    ?? "???";
                var senderName =
                    Environment.GetEnvironmentVariable("BREVO_SENDER_NAME")
                    ?? _configuration["Email:SenderName"]
                    ?? "NaPos'o Platforma";

                _logger.LogInformation("Test email poslan na {To}. Elapsed={Elapsed}ms", to, sw.ElapsedMilliseconds);

                return Ok(new
                {
                    success = true,
                    message = "Test email je proslijeđen Brevo API-ju (HTTP 2xx). Ako email ne stigne odmah, provjeri spam folder ili Brevo Logs (app.brevo.com/campaigns/transactional/Logs).",
                    @to = to,
                    toName = name,
                    subject,
                    sender = new { name = senderName, email = senderEmail },
                    endpoint = "POST https://api.brevo.com/v3/smtp/email",
                    authHeader = "api-key",
                    elapsedMs = sw.ElapsedMilliseconds,
                    timestamp = DateTimeOffset.Now.ToString("o")
                });
            }
            catch (HttpRequestException httpEx)
            {
                sw.Stop();
                _logger.LogError(httpEx, "Test email NIJE uspio za {To}. HTTP status={Status}", to, httpEx.StatusCode);
                return StatusCode((int?)httpEx.StatusCode ?? 500, new
                {
                    success = false,
                    stage = "Brevo API",
                    httpStatus = (int?)httpEx.StatusCode ?? 0,
                    httpStatusName = httpEx.StatusCode?.ToString() ?? "n/a",
                    elapsedMs = sw.ElapsedMilliseconds,
                    error = httpEx.Message,
                    hint = PopuniHint((int?)httpEx.StatusCode ?? 0, httpEx.Message)
                });
            }
            catch (InvalidOperationException ioEx)
            {
                sw.Stop();
                _logger.LogError(ioEx, "Test email NIJE uspio za {To} — konfiguracijska greška.", to);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    stage = "Konfiguracija",
                    elapsedMs = sw.ElapsedMilliseconds,
                    error = ioEx.Message,
                    hint = "Popuni BREVO_API_KEY i BREVO_SENDER_EMAIL u .env fajlu u root-u projekta (gdje je i .gitignore)."
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Test email NIJE uspio za {To}.", to);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    stage = "Neocekivan izuzetak",
                    elapsedMs = sw.ElapsedMilliseconds,
                    error = ex.GetType().Name + ": " + ex.Message,
                    hint = "Pogledaj app log (console) za stack trace."
                });
            }
        }

        private static string PopuniHint(int httpStatus, string msg)
        {
            var low = msg.ToLowerInvariant();
            switch (httpStatus)
            {
                case 401:
                case 403:
                    return "Neispravan BREVO_API_KEY. Idi na https://app.brevo.com/settings/keys/api i napravi NOVI key, zalijepi ga u .env BREVO_API_KEY=...";
                case 400 when low.Contains("sender"):
                    return "BREVO_SENDER_EMAIL nije verifikovan na Brevu. Idi https://app.brevo.com/settings/senders , klikni Resend verification pa potvrdi link u inboxu.";
                case 400 when low.Contains("invalid_parameter"):
                    return "Brevo kaže invalid_parameter. Najčešće: neispravan format emaila ili nedostaje sender. Provjeri BREVO_SENDER_EMAIL i 'to' parametar.";
                case 429:
                    return "Brevo rate limit (previse emaila brzo). Pricekaj nekoliko sekundi i pokusaj ponovo.";
                default:
                    return "Provjeri .env vrijednosti (BREVO_API_KEY, BREVO_SENDER_EMAIL, BREVO_SENDER_NAME). Brevo Logs: https://app.brevo.com/campaigns/transactional/Logs";
            }
        }
    }
}
