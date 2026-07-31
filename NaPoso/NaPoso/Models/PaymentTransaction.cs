using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded,
        Held,       // Novac naplaćen ali čeka potvrdu posla (escrow)
        Released    // Transfer poslat radniku
    }

    public class PaymentTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int? OglasId { get; set; }

        [Required]
        public string StripePaymentIntentId { get; set; } = string.Empty;

        public string? StripeEventId { get; set; }

        public long Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "usd";

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Stripe Connect — transfer radniku
        public string? TransferId { get; set; }
        public long? PlatformFeeAmount { get; set; }
        public string? WorkerUserId { get; set; }

        // Stripe Checkout Session ID — used for idempotent transaction creation
        // so that the Success page can reliably create/find transactions without TempData
        public string? StripeSessionId { get; set; }

        // Eksplicitno sačuvan bakšiš (nagrada za radnika) u feningima
        // (Amount = Osnova + TipAmountFeninga; provizija se računa samo od osnove)
        public long TipAmountFeninga { get; set; } = 0;
    }
}
