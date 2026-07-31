using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using NaPoso.Data;
using NaPoso.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using static NaPoso.Enums.Enums;

namespace NaPoso.Areas.Identity.Pages.Payment
{
    public class SuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeService _stripeService;
        private readonly ILogger<SuccessModel> _logger;

        public SuccessModel(ApplicationDbContext context, StripeService stripeService, ILogger<SuccessModel> logger)
        {
            _context = context;
            _stripeService = stripeService;
            _logger = logger;
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

                _logger.LogInformation(
                    "[Payment/Success] OnGet pozvan. session_id={Sid}, UserIdentityName={Usr}",
                    session_id ?? "<NULL>", CustomerEmail ?? "<NULL>");

                // ================================================================
                // STRIPE SESSION_ID IS THE SOLE SOURCE OF TRUTH
                // All payment data (OglasId, RadnikId, TipAmount, Amount) is read
                // DIRECTLY from Stripe via session_id from the query string.
                // This is independent of TempData/Session/DataProtection keys.
                // ================================================================
                if (!string.IsNullOrEmpty(session_id) && _stripeService.IsConfigured)
                {
                    try
                    {
                        var session = await _stripeService.GetSessionAsync(session_id);
                        if (session == null)
                        {
                            _logger.LogWarning("[Payment/Success] Stripe session je NULL za session_id={Sid}", session_id);
                            DebugInfo = "Stripe sesija nije pronađena.";
                            return Page();
                        }

                        _logger.LogInformation(
                            "[Payment/Success] Session fetched from Stripe. Id={SessionId}, PiId={PiId}, " +
                            "AmountTotal={AmtTotal}, Currency={Cur}, Status={Status}, MetadataCount={MetaCount}",
                            session.Id, session.PaymentIntentId, session.AmountTotal,
                            session.Currency, session.PaymentStatus, session.Metadata?.Count ?? 0);

                        // Parse all metadata from Stripe session
                        string mdUserId = null;
                        int? mdOglasId = null;
                        string mdRadnikId = null;
                        long mdTipAmountFeninga = 0;

                        if (session.Metadata != null)
                        {
                            foreach (var kvp in session.Metadata)
                            {
                                _logger.LogInformation(
                                    "[Payment/Success] Metadata KVP: Key='{Key}', Value='{Value}'",
                                    kvp.Key, kvp.Value);
                            }

                            if (session.Metadata.TryGetValue("UserId", out var v))
                                mdUserId = v;
                            if (session.Metadata.TryGetValue("OglasId", out var oglasStr)
                                && int.TryParse(oglasStr, out var oglasParsed))
                                mdOglasId = oglasParsed;
                            if (session.Metadata.TryGetValue("RadnikId", out var r))
                                mdRadnikId = r;

                            // Check all possible tip key names
                            foreach (var kvp in session.Metadata)
                            {
                                if (kvp.Key.Equals("TipAmountFeninga", StringComparison.OrdinalIgnoreCase) ||
                                    kvp.Key.Equals("BaksisFeninga", StringComparison.OrdinalIgnoreCase) ||
                                    kvp.Key.Equals("TipAmount", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (long.TryParse(kvp.Value, out var tipParsed))
                                    {
                                        mdTipAmountFeninga = tipParsed;
                                        _logger.LogInformation(
                                            "[Payment/Success] TipAmount found: Key={Key}, Value={TipFeninga}",
                                            kvp.Key, tipParsed);
                                    }
                                }
                            }
                        }

                        _logger.LogInformation(
                            "[Payment/Success] Metadata parsed: UserId={U}, OglasId={O}, RadnikId={R}, TipAmountFeninga={Tip}",
                            mdUserId, mdOglasId, mdRadnikId, mdTipAmountFeninga);

                        // ============================================================
                        // IDEMPOTENT TRANSACTION CREATION using StripeSessionId
                        // Check if a PaymentTransaction already exists for this session_id.
                        // If yes, skip creation (prevents duplicates on page refresh).
                        // If no, create a new one with data directly from Stripe.
                        // ============================================================
                        var existingTx = await _context.PaymentTransactions
                            .FirstOrDefaultAsync(pt => pt.StripeSessionId == session_id);

                        if (existingTx != null)
                        {
                            _logger.LogInformation(
                                "[Payment/Success] Transaction already exists for session_id={Sid}. " +
                                "TxId={TxId}, Amount={Amt}, TipAmountFeninga={Tip}, Status={Status}",
                                session_id, existingTx.Id, existingTx.Amount,
                                existingTx.TipAmountFeninga, existingTx.Status);

                            // Use existing data for view
                            OglasId = existingTx.OglasId;
                            RadnikId = existingTx.WorkerUserId;
                        }
                        else
                        {
                            // Also check by PaymentIntentId (may have been created by webhook)
                            var txByPi = !string.IsNullOrEmpty(session.PaymentIntentId)
                                ? await _context.PaymentTransactions
                                    .FirstOrDefaultAsync(pt => pt.StripePaymentIntentId == session.PaymentIntentId)
                                : null;

                            if (txByPi != null)
                            {
                                // Transaction exists from webhook but missing StripeSessionId — update it
                                txByPi.StripeSessionId = session_id;
                                if (string.IsNullOrEmpty(txByPi.UserId) && !string.IsNullOrEmpty(mdUserId))
                                    txByPi.UserId = mdUserId;
                                if (!txByPi.OglasId.HasValue && mdOglasId.HasValue)
                                    txByPi.OglasId = mdOglasId.Value;
                                if (string.IsNullOrEmpty(txByPi.WorkerUserId) && !string.IsNullOrEmpty(mdRadnikId))
                                    txByPi.WorkerUserId = mdRadnikId;
                                if (txByPi.TipAmountFeninga == 0 && mdTipAmountFeninga > 0)
                                    txByPi.TipAmountFeninga = mdTipAmountFeninga;
                                if (txByPi.Amount == 0 && session.AmountTotal.HasValue)
                                {
                                    txByPi.Amount = session.AmountTotal.Value;
                                    txByPi.Currency = session.Currency ?? "usd";
                                }
                                else if (session.AmountTotal.HasValue && session.AmountTotal.Value > txByPi.Amount)
                                {
                                    txByPi.Amount = session.AmountTotal.Value;
                                }
                                if (session.PaymentStatus == "paid")
                                {
                                    txByPi.Status = Models.PaymentStatus.Paid;
                                    txByPi.PaidAt = txByPi.PaidAt ?? DateTime.UtcNow;
                                }
                                txByPi.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();

                                _logger.LogInformation(
                                    "[Payment/Success] Updated existing webhook-created transaction. " +
                                    "TxId={TxId}, StripeSessionId={Sid}, Amount={Amt}, TipAmountFeninga={Tip}",
                                    txByPi.Id, session_id, txByPi.Amount, txByPi.TipAmountFeninga);

                                OglasId = txByPi.OglasId;
                                RadnikId = txByPi.WorkerUserId;
                            }
                            else
                            {
                                // Create NEW transaction entirely from Stripe data
                                var newTx = new Models.PaymentTransaction
                                {
                                    StripeSessionId = session_id,
                                    StripePaymentIntentId = session.PaymentIntentId ?? string.Empty,
                                    StripeEventId = string.Empty,
                                    UserId = mdUserId ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                                    OglasId = mdOglasId,
                                    WorkerUserId = mdRadnikId,
                                    TipAmountFeninga = mdTipAmountFeninga,
                                    Amount = session.AmountTotal ?? 0,
                                    Currency = session.Currency ?? "usd",
                                    Status = session.PaymentStatus == "paid"
                                        ? Models.PaymentStatus.Paid
                                        : Models.PaymentStatus.Pending,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    PaidAt = session.PaymentStatus == "paid" ? DateTime.UtcNow : null
                                };
                                _context.PaymentTransactions.Add(newTx);
                                await _context.SaveChangesAsync();

                                _logger.LogInformation(
                                    "[Payment/Success] NEW transaction created from Stripe data. " +
                                    "TxId={TxId}, StripeSessionId={Sid}, Amount={Amt}, " +
                                    "TipAmountFeninga={Tip}, OglasId={OId}, Status={Status}",
                                    newTx.Id, session_id, newTx.Amount,
                                    newTx.TipAmountFeninga, newTx.OglasId, newTx.Status);

                                OglasId = newTx.OglasId;
                                RadnikId = newTx.WorkerUserId;
                            }
                        }

                        // ============================================================
                        // SET Oglas.Status = Zavrsen and OglasKorisnik.Status = Zavrsen
                        // directly from Stripe metadata, NOT from TempData.
                        // ============================================================
                        if (mdOglasId.HasValue && !string.IsNullOrEmpty(mdRadnikId))
                        {
                            _logger.LogInformation(
                                "[Payment/Success] Setting Oglas/OglasKorisnik STATUS = Zavrsen. " +
                                "OglasId={OId}, RadnikId={RId}",
                                mdOglasId.Value, mdRadnikId);

                            var prijava = await _context.OglasKorisnik
                                .FirstOrDefaultAsync(ok => ok.OglasId == mdOglasId.Value && ok.KorisnikId == mdRadnikId);

                            if (prijava != null &&
                                prijava.Status != Enums.Enums.Status.Zavrsen &&
                                prijava.Status != Enums.Enums.Status.Placen)
                            {
                                prijava.Status = Enums.Enums.Status.Zavrsen;
                                _logger.LogInformation(
                                    "[Payment/Success] OglasKorisnik.Status → Zavrsen (OglasId={OId}, KorisnikId={KId})",
                                    mdOglasId.Value, mdRadnikId);
                            }

                            var oglas = await _context.Oglas
                                .FirstOrDefaultAsync(o => o.Id == mdOglasId.Value);
                            if (oglas != null &&
                                oglas.Status != Enums.Enums.Status.Zavrsen &&
                                oglas.Status != Enums.Enums.Status.Placen)
                            {
                                oglas.Status = Enums.Enums.Status.Zavrsen;
                                _logger.LogInformation(
                                    "[Payment/Success] Oglas.Status → Zavrsen (OglasId={OId}, Naslov='{Naslov}')",
                                    mdOglasId.Value, oglas.Naslov);
                            }

                            try { await _context.SaveChangesAsync(); }
                            catch (Exception sx)
                            {
                                _logger.LogWarning(sx, "[Payment/Success] SaveChanges for Oglas/OglasKorisnik status update failed.");
                            }
                        }

                        // Set session vars for downstream (RecenzijaController) compatibility
                        if (mdOglasId.HasValue)
                        {
                            HttpContext.Session.SetString("PaymentVerified", "true");
                            HttpContext.Session.SetInt32("VerifiedOglasId", mdOglasId.Value);
                            if (!string.IsNullOrEmpty(mdRadnikId))
                                HttpContext.Session.SetString("VerifiedRadnikId", mdRadnikId);
                        }
                    }
                    catch (Exception sx)
                    {
                        _logger.LogWarning(sx, "[Payment/Success] Failed to process Stripe session.");
                        DebugInfo = $"Greška pri obradi Stripe sesije: {sx.Message}";
                    }
                }
                else if (string.IsNullOrEmpty(session_id))
                {
                    _logger.LogWarning("[Payment/Success] session_id is NULL/empty in query string!");
                    DebugInfo = "Nedostaje session_id iz Stripe redirect-a.";
                }
                else
                {
                    _logger.LogWarning("[Payment/Success] Stripe is not configured.");
                    DebugInfo = "Stripe nije konfigurisan.";
                }
            }
            catch (Exception ex)
            {
                DebugInfo = $"Greška: {ex.Message}";
                _logger.LogError(ex, "[Payment/Success] FATAL GRESKA u OnGetAsync.");
            }

            return Page();
        }

    }
}