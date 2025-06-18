using Microsoft.AspNetCore.Identity;
using NaPoso.Enums;
using static NaPoso.Enums.Enums;

namespace NaPoso.Models
{
    public class OglasKorisnik
    {
        public int Id { get; set; }

        public int? OglasId { get; set; }
        public Oglas Oglas { get; set; }

        public string? KorisnikId { get; set; }
        public Korisnik Korisnik { get; set; }

        public Status Status { get; set; } = Status.Aktivan;

        public DateTime DatumPrijave { get; set; } = DateTime.Now;


    }
}
