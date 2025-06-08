using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public class AdminOglasView
    {
        [Required, EmailAddress]
        [Display(Name = "Email klijenta")]
        public string KlijentEmail { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Naslov { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Opis { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Lokacija { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Tip posla")]
        public string TipPosla { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Cijena mora biti veća od 0")]
        [Display(Name = "Cijena posla")]
        public double CijenaPosla { get; set; }
    }
}
