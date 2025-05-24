using NaPoso.Enums;

namespace NaPoso.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat Chat { get; set; }

        public string PosiljaocId { get; set; }
        public ApplicationUser Posiljaoc { get; set; }

        public string? Text { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}
