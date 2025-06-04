using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NaPoso.Services;
using NaPoso.Data;
using NaPoso.Models;
using static NaPoso.Enums.Enums;

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
        public bool WorkerAccepted { get; set; }

        public async Task<IActionResult> OnGetAsync(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                return RedirectToPage("/Index");
            }

            var session = await _stripeService.GetSessionAsync(session_id);
            PaymentStatus = session.PaymentStatus;
            CustomerEmail = session.CustomerDetails?.Email;

            if (session.PaymentStatus == "paid")
            {
                if (TempData["PrihvatiOglasId"] != null &&
                    TempData["PrihvatiRadnikId"] != null)
                {
                    int oglasId = (int)TempData["PrihvatiOglasId"];
                    string radnikId = TempData["PrihvatiRadnikId"].ToString();

                    var oglas = await _context.Oglas.FindAsync(oglasId);
                    if (oglas != null)
                    {
                        oglas.RadnikId = radnikId;
                        oglas.Status = Status.Neaktivan;
                        await _context.SaveChangesAsync();

                        OglasId = oglasId;
                        OglasNaslov = oglas.Naslov;
                        WorkerAccepted = true;
                    }
                }
            }

            return Page();
        }
    }
}