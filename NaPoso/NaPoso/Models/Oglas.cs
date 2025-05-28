using System.ComponentModel.DataAnnotations;
using static NaPoso.Enums.Enums;

namespace NaPoso.Models
{
    public class Oglas
    {
        public int Id { get; set; }
        public string? KlijentId { get; set; }
        public string? RadnikId { get; set; }
        public string? Opis { get; set; }
        public string? Lokacija { get; set; }
        [Display(Name = "Tip posla")]
        public string? TipPosla {  get; set; }
        [Display(Name = "Cijena posla")]
        public double CijenaPosla { get; set; }
        public string? Naslov {  get; set; }
        public Recenzija? Recenzija {  get; set; }  
        public Status Status { get; set; }
    }
}
