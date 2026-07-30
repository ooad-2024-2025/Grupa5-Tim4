using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public class CreateAdminViewModel
    {
        [Required(ErrorMessage = "Ime je obavezno.")]
        public string Ime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime je obavezno.")]
        public string Prezime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Nevaljan format emaila.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Šifra je obavezna.")]
        [StringLength(100, ErrorMessage = "Šifra mora imati barem {2} i najviše {1} karaktera.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Potvrdi šifru")]
        [Compare("Password", ErrorMessage = "Šifre se ne podudaraju.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
