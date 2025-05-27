using Microsoft.AspNetCore.Identity;
using NaPoso.Enums;

namespace NaPoso.Models
{
    public class Chat 
    {
        public int Id { get; set; }

        public string KlijentId { get; set; }
        public IdentityUser Klijent { get; set; }

        public string RadnikId { get; set; }
        public IdentityUser Radnik { get; set; }

    }
}
