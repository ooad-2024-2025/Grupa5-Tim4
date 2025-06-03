using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NaPoso.Services;
using NaPoso.Data;

namespace NaPoso.Areas.Identity.Pages.Payment
{
    public class SuccessModel : PageModel
    {
        private readonly StripeService _stripeService;
        private readonly ApplicationDbContext _context;

        public SuccessModel(StripeService stripeService, ApplicationDbContext context)
        {
            _stripeService = stripeService;
            _context = context;
        }

        public string PaymentStatus { get; set; }
        public string CustomerEmail { get; set; }
        public int? OglasId { get; set; }
        public string OglasNaslov { get; set; }

        public async Task<IActionResult> OnGetAsync(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                return RedirectToPage("/Index");
            }

            var session = await _stripeService.GetSessionAsync(session_id);
            PaymentStatus = session.PaymentStatus;
            CustomerEmail = session.CustomerDetails?.Email;

            // Complete job payment process if payment successful
            if (session.PaymentStatus == "paid" && TempData["OglasId"] != null)
            {
                if (int.TryParse(TempData["OglasId"].ToString(), out int oglasId))
                {
                    OglasId = oglasId;
                    var oglas = await _context.Oglas.FindAsync(oglasId);
                    if (oglas != null)
                    {
                        // Add a marker field for payment if you need it
                        // oglas.IsPlacen = true;
                        OglasNaslov = oglas.Naslov;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Page();
        }
    }
}