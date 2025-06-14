using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public class Recenzija
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        [Range(1, 5, ErrorMessage = "Ocjena mora biti između 1 i 5.")]
        public int Ocjena { get; set; }
        [Required(ErrorMessage = "Ovo polje je obavezno.")]
        public string Sadrzaj { get; set; }

        public string KlijentId { get; set; }  

        public string RadnikId { get; set; }  
    }
}
