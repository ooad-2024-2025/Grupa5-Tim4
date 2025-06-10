using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using NaPoso.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using static NaPoso.Enums.Enums;

namespace NaPoso.Areas.Identity.Pages.Payment
{
    public class SuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SuccessModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string PaymentStatus { get; set; }
        public string CustomerEmail { get; set; }
        public int? OglasId { get; set; }
        public string RadnikId { get; set; }
        public string DebugInfo { get; set; }

        public async Task<IActionResult> OnGetAsync(string session_id)
        {
            try
            {
                PaymentStatus = "Plaćanje uspješno";
                CustomerEmail = User.Identity?.Name;

                if (TempData.ContainsKey("OglasId") && TempData["OglasId"] != null &&
                    TempData.ContainsKey("RadnikId") && TempData["RadnikId"] != null)
                {
                    OglasId = Convert.ToInt32(TempData["OglasId"]);
                    RadnikId = TempData["RadnikId"].ToString();

                    TempData.Keep("OglasId");
                    TempData.Keep("RadnikId");

                    // Pronađi prijavu tog korisnika na oglas
                    var prijava = await _context.OglasKorisnik
                        .FirstOrDefaultAsync(ok => ok.OglasId == OglasId && ok.KorisnikId == RadnikId);

                    if (prijava != null)
                    {
                        prijava.Status = Status.Placen; // postavi status na placen
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        DebugInfo = "Prijava korisnika za oglas nije pronađena.";
                    }

                    // Postavi sesiju ako je potrebno
                    HttpContext.Session.SetString("PaymentVerified", "true");
                    HttpContext.Session.SetInt32("VerifiedOglasId", OglasId.Value);
                    HttpContext.Session.SetString("VerifiedRadnikId", RadnikId);
                }
                else
                {
                    DebugInfo = "Nedostaju podaci za OglasId ili RadnikId.";
                }
            }
            catch (Exception ex)
            {
                DebugInfo = $"Greška: {ex.Message}";
            }

            return Page();
        }

    }
}