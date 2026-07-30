using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NaPoso.Models
{
    public class PrijavaRecenzije
    {
        public int Id { get; set; }
        
        [Required]
        public int RecenzijaId { get; set; }
        public Recenzija? Recenzija { get; set; }

        [Required]
        public string? PrijavioKorisnikId { get; set; }
        public Korisnik? PrijavioKorisnik { get; set; }

        [Required(ErrorMessage = "Razlog prijave je obavezan.")]
        public string? Razlog { get; set; }

        public DateTime DatumPrijave { get; set; } = DateTime.UtcNow;

        // true = rijeseno, false = otvoreno
        public bool JeRijeseno { get; set; } = false;
    }
}
