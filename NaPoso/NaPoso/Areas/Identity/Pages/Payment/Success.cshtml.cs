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
                // Set payment status
                PaymentStatus = "Plaćanje uspješno";

                // Get customer email from user if authenticated
                CustomerEmail = User.Identity?.Name;

                // Safely get OglasId from TempData
                if (TempData.ContainsKey("OglasId") && TempData["OglasId"] != null)
                {
                    try
                    {
                        OglasId = Convert.ToInt32(TempData["OglasId"]);
                        TempData.Keep("OglasId");

                        // Update Oglas status to Neaktivan after successful payment
                        if (OglasId.HasValue)
                        {
                            var oglas = await _context.Oglas.FindAsync(OglasId.Value);
                            if (oglas != null)
                            {
                                oglas.Status = Status.Neaktivan; // Change status to inactive
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugInfo = $"Failed to update Oglas status: {ex.Message}";
                    }
                }

                // Safely get RadnikId from TempData
                if (TempData.ContainsKey("RadnikId") && TempData["RadnikId"] != null)
                {
                    RadnikId = TempData["RadnikId"].ToString();
                    TempData.Keep("RadnikId");
                }

                // Only store session data if we have both values
                if (OglasId.HasValue && !string.IsNullOrEmpty(RadnikId))
                {
                    try
                    {
                        // Simple token to verify the payment was completed
                        HttpContext.Session.SetString("PaymentVerified", "true");
                        HttpContext.Session.SetInt32("VerifiedOglasId", OglasId.Value);
                        HttpContext.Session.SetString("VerifiedRadnikId", RadnikId);
                    }
                    catch (Exception ex)
                    {
                        // If session fails, log the error but don't crash
                        DebugInfo = $"Error setting session: {ex.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch all errors to prevent crashes
                DebugInfo = $"Unexpected error: {ex.Message}";
            }

            return Page();
        }
    }
}