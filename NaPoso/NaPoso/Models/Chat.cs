using NaPoso.Enums;

namespace NaPoso.Models
{
    public class Chat 
    {
        public int Id { get; set; }

        public string? KlijentId { get; set; }
        public ApplicationUser Klijent { get; set; }

        public string RadnikId { get; set; }
        public ApplicationUser Radnik { get; set; }

    }
}
