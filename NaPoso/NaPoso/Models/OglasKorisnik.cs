using Microsoft.AspNetCore.Identity;

namespace NaPoso.Models
{
    public class OglasKorisnik
    {
        public int Id { get; set; }

        public int OglasId { get; set; }
        public Oglas Oglas { get; set; }

        public string? KorisnikId { get; set; }
        public IdentityUser Korisnik { get; set; }

        public DateTime DatumPrijave { get; set; } = DateTime.Now;


    }
}
