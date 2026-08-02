using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NaPoso.Models
{
    public class PrijavaOglasa
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OglasId { get; set; }

        [ForeignKey("OglasId")]
        public virtual Oglas? Oglas { get; set; }

        [Required]
        public string PrijavioKorisnikId { get; set; } = string.Empty;

        [ForeignKey("PrijavioKorisnikId")]
        public virtual Korisnik? PrijavioKorisnik { get; set; }

        [Required(ErrorMessage = "Razlog prijave je obavezan.")]
        [StringLength(1000)]
        public string Razlog { get; set; } = string.Empty;

        public DateTime DatumPrijave { get; set; } = DateTime.UtcNow;

        public bool JeRijeseno { get; set; } = false;
    }
}
