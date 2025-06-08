using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public class Korisnik : IdentityUser
    {
        public string? Ime { get; set; }
        public string? Prezime { get; set; }
        [Display(Name = "Broj telefona")]
        public string? BrojTelefona;
        public bool Verified { get; set; } = false;
    }
}
