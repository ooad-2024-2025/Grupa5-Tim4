using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NaPoso.Services;
using System.ComponentModel.DataAnnotations;

namespace NaPoso.Areas.Identity.Pages.Payment
{
    public class CheckoutModel : PageModel
    {
        private readonly StripeService _stripeService;
        private readonly IConfiguration _configuration;

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

        public CheckoutModel(StripeService stripeService, IConfiguration configuration)
        {
            _stripeService = stripeService;
            _configuration = configuration;
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
                var session = await _stripeService.CreateCheckoutSessionAsync(ProductName, Amount);
                if (session == null || string.IsNullOrEmpty(session.Url))
                {
                    ModelState.AddModelError(string.Empty, "Nije moguće kreirati sesiju plaćanja. Pokušajte ponovo.");
                    return Page();
                }
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