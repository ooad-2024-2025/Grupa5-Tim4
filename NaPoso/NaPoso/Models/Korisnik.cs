using Microsoft.AspNetCore.Identity;

namespace NaPoso.Models
{
    public class Korisnik : IdentityUser
    {
        public string? Ime {  get; set; }
        public string? Prezime { get; set; }
        
    }
}
