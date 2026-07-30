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

        [Required]
        public string StripeEventId { get; set; } = string.Empty;

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
    }
}
