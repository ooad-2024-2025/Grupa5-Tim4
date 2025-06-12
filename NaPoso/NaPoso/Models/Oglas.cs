using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static NaPoso.Enums.Enums;

namespace NaPoso.Models
{
    public class Oglas
    {
        public int Id { get; set; }
    
        public string? KlijentId { get; set; }
        public string? RadnikId { get; set; }
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Naslov mora imati između 3 i 100 znakova.")]
        public string? Naslov { get; set; }

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Opis mora imati između 3 i 100 znakova.")]
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        public string? Opis { get; set; }
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Lokacija mora imati između 1 i 100 znakova.")]
        [RegularExpression(@"^[A-Za-z ]+$", ErrorMessage = "Lokacija mora sadržavati samo slova.")]
        public string? Lokacija { get; set; }
        [Display(Name = "Tip posla")]
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        [RegularExpression(@"^[A-Za-z ]+$", ErrorMessage ="Tip posla mora sadržavati samo slova.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tip posla mora imati između 3 i 100 znakova.")]
        public string? TipPosla {  get; set; }
        [Display(Name = "Cijena posla")]
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        [Range(1, int.MaxValue, ErrorMessage = "Cijena mora biti veća od 0.")]
        public double CijenaPosla { get; set; }
        public Recenzija? Recenzija {  get; set; }  
        public Status Status { get; set; }
    }
}
