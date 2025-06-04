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
        [Display(Name = "Iznos (u centima)")]
        public long Amount { get; set; } = 0; 

        public CheckoutModel(StripeService stripeService, IConfiguration configuration)
        {
            _stripeService = stripeService;
            _configuration = configuration;
        }

        public string PublishableKey => _configuration["Stripe:PublishableKey"];

        public void OnGet(string productName = null, long? amount = null)
        {
            if (!string.IsNullOrEmpty(productName))
            {
                ProductName = productName;
            }

            if (amount.HasValue)
            {
                Amount = amount.Value;
            }
            else
            {
                Amount = 0; 
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Amount < 50 || Amount > 999999999999)
            {
                ModelState.AddModelError("Amount", "Iznos mora biti izmedju 50 i 9,999,999,999.99");
                return Page();
            }

            try
            {
                var session = await _stripeService.CreateCheckoutSessionAsync(ProductName, Amount);
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