using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public class AdminOglasView
    {
        [Display(Name = "Email klijenta")]
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email mora biti u ispravnom formatu (npr. korisnik@example.com).")]
        public string KlijentEmail { get; set; }

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
        [RegularExpression(@"^[A-Za-z ]+$", ErrorMessage = "Tip posla mora sadržavati samo slova.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tip posla mora imati između 3 i 100 znakova.")]
        public string? TipPosla { get; set; }
        [Display(Name = "Cijena posla")]
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        [Range(0.5, 9999999, ErrorMessage = "Cijena može biti najmanje 0.50KM i najviše 9999999KM.")]
        [RegularExpression(@"^-?\d+(\.\d{1,2})?$", ErrorMessage = "Unesite broj sa najviše dvije decimale (tačka kao decimalni separator).")]
        public double CijenaPosla { get; set; }
    }
}
