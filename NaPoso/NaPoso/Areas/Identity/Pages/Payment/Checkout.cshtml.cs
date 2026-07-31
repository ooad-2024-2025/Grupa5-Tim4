using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NaPoso.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace NaPoso.Areas.Identity.Pages.Payment
{
    public class CheckoutModel : PageModel
    {
        private readonly StripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CheckoutModel> _logger;

        [BindProperty]
        [Required(ErrorMessage = "Naziv proizvoda/usluge je obavezan")]
        public string ProductName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Iznos je obavezan")]
        [Range(50, 999999999999, ErrorMessage = "Iznos mora biti izmedju 50 i 9,999,999,999.99")]
        [Display(Name = "Iznos u feningama (osnova + baksis)")]
        public long Amount { get; set; } = 0;

        [Display(Name = "Osnovna cijena posla")]
        public string CijenaOglasaKm { get; set; } = "0.00 KM";
        public long CijenaOglasaFeninga { get; set; } = 0;

        [BindProperty]
        [Range(0, 99999999)]
        [Display(Name = "Baksis / ekstra iznos za radnika")]
        public long BaksisFeninga { get; set; } = 0;
        public string BaksisKm { get; set; } = "0.00 KM";

        [Display(Name = "UKUPNO ZA PLAĆANJE")]
        public string UkupnoKm { get; set; } = "0.00 KM";
        public long UkupnoFeninga { get; set; } = 0;

        [BindProperty]
        public int? OglasId { get; set; }

        [BindProperty]
        public string? RadnikId { get; set; }

        public CheckoutModel(StripeService stripeService, IConfiguration configuration, ILogger<CheckoutModel> logger)
        {
            _stripeService = stripeService;
            _configuration = configuration;
            _logger = logger;
        }

        public string? PublishableKey => _configuration["Stripe:PublishableKey"];

        public void OnGet(string productName = null, long? amount = null, decimal? amountKm = null)
        {
            if (!string.IsNullOrEmpty(productName))
            {
                ProductName = productName;
            }

            long bazaFeninga = 0;
            decimal bazaKm = 0m;

            if (amountKm.HasValue)
            {
                bazaKm = amountKm.Value;
                bazaFeninga = amount.HasValue ? amount.Value : (long)Math.Round(amountKm.Value * 100);
            }
            else if (amount.HasValue)
            {
                bazaFeninga = amount.Value;
                bazaKm = amount.Value / 100.0m;
            }

            CijenaOglasaFeninga = bazaFeninga;
            CijenaOglasaKm = $"{bazaKm:0.00} KM";

            BaksisFeninga = 0;
            BaksisKm = "0.00 KM";

            UkupnoFeninga = bazaFeninga;
            UkupnoKm = $"{bazaKm:0.00} KM";
            Amount = bazaFeninga;

            if (TempData.ContainsKey("OglasId") && TempData["OglasId"] != null)
            {
                OglasId = Convert.ToInt32(TempData["OglasId"]);
                TempData.Keep("OglasId");
            }

            if (TempData.ContainsKey("RadnikId") && TempData["RadnikId"] != null)
            {
                RadnikId = TempData["RadnikId"].ToString();
                TempData.Keep("RadnikId");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Amount < CijenaOglasaFeninga || Amount > 999999999999)
            {
                ModelState.AddModelError(nameof(Amount), $"Iznos ne smije biti manji od cijene posla ({CijenaOglasaKm}).");
                return Page();
            }

            if (!_stripeService.IsConfigured)
            {
                ModelState.AddModelError(string.Empty, "Plaćanje nije konfigurisano. Stripe API key nedostaje. Kontaktirajte administratora.");
                return Page();
            }

            try
            {
                var metadata = new Dictionary<string, string>();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    metadata["UserId"] = userId;
                }

                if (OglasId.HasValue)
                {
                    metadata["OglasId"] = OglasId.Value.ToString();
                    TempData["OglasId"] = OglasId.Value;
                }

                if (!string.IsNullOrEmpty(RadnikId))
                {
                    metadata["RadnikId"] = RadnikId;
                    TempData["RadnikId"] = RadnikId;
                }

                metadata["TipAmountFeninga"] = BaksisFeninga.ToString();

                _logger.LogInformation(
                    "[Checkout OnPost] Priprema za Stripe Checkout: " +
                    "ProductName={Prod}, Amount_Feninga={AmtF} (={AmtKM:.00} KM), " +
                    "BaksisFeninga={BakF} (={BakKM:.00} KM), CijenaOglasaFeninga={CijenaF}, " +
                    "OglasId={OId}, RadnikId={RId}, UserId={UId}, " +
                    "Metadata_TipAmountFeninga_Key='{Key}', Metadata_TipAmountFeninga_Value='{Val}'",
                    ProductName, Amount, (decimal)Amount / 100m,
                    BaksisFeninga, (decimal)BaksisFeninga / 100m, CijenaOglasaFeninga,
                    OglasId, RadnikId, userId,
                    "TipAmountFeninga", BaksisFeninga.ToString());

                var session = await _stripeService.CreateCheckoutSessionAsync(
                    ProductName,
                    Amount,
                    metadata: metadata);

                if (session == null || string.IsNullOrEmpty(session.Url))
                {
                    ModelState.AddModelError(string.Empty, "Nije moguće kreirati sesiju plaćanja. Pokušajte ponovo.");
                    _logger.LogError("[Checkout OnPost] Stripe session je NULL ili nema URL. ProductName={Prod}, Amount={Amt}",
                        ProductName, Amount);
                    return Page();
                }

                _logger.LogInformation(
                    "[Checkout OnPost] Stripe Checkout Session kreirana. SessionId={Sid}, URL={Url}, " +
                    "PaymentIntentId={PiId}, AmountSubmitted={AmtF}, MetadataBaksis={BakF}",
                    session.Id, session.Url, session.PaymentIntentId, Amount, BaksisFeninga);

                return Redirect(session.Url);
            }
            catch (Stripe.StripeException ex)
            {
                ModelState.AddModelError(string.Empty, $"Greska pri obradi placanja: {ex.Message}");
                return Page();
            }
        }
    }
}