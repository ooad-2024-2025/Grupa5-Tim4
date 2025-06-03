using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NaPoso.Services;

namespace NaPoso.Areas.Identity.Pages.Payment
{
    public class CheckoutModel : PageModel
    {
        private readonly StripeService _stripeService;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string ProductName { get; set; }

        [BindProperty]
        public long Amount { get; set; }

        public CheckoutModel(StripeService stripeService, IConfiguration configuration)
        {
            _stripeService = stripeService;
            _configuration = configuration;
        }

        public string PublishableKey => _configuration["Stripe:PublishableKey"];

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var session = await _stripeService.CreateCheckoutSessionAsync(ProductName, Amount);
            return Redirect(session.Url);
        }
    }
}