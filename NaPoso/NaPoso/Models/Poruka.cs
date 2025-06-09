// Models/Poruka.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NaPoso.Models
{
    public class Poruka
    {
        public int Id { get; set; }

        [Required]
        public int ChatId { get; set; }
        public Chat Chat { get; set; }

        [Required]
        public string PosiljaocId { get; set; }
        public Korisnik Posiljaoc { get; set; }

        [Required]
        public string Tekst { get; set; }

        public DateTime PoslanoAt { get; set; } = DateTime.Now;
    }
}
