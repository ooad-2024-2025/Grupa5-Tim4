// Models/Chat.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NaPoso.Models
{
    public class Chat
    {
        public int Id { get; set; }

        [Required]
        public string Korisnik1Id { get; set; }
        public Korisnik Korisnik1 { get; set; }

        [Required]
        public string Korisnik2Id { get; set; }
        public Korisnik Korisnik2 { get; set; }

        public int OglasId { get; set; }
        public Oglas Oglas { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Poruka> Poruke { get; set; }
    }
}
