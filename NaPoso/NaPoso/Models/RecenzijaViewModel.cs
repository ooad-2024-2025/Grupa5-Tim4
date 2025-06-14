using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public class RecenzijaViewModel
    {
        public string KlijentEmail { get; set; }
        [Range(1, 5, ErrorMessage = "Ocjena mora biti između 1 i 5.")]
        public int Ocjena { get; set; }
        public string Sadrzaj { get; set; }
    }
}
