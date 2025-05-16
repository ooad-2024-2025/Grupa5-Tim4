using static NaPoso.Enums.Enums;

namespace NaPoso.Models
{
    public class Obavijest
    {
        public int Id { get; set; }
        public int KorisnikId {  get; set; }
        public string Sadrzaj { get; set; }
        public DateTime VrijemeSlanja { get; set; }
        public Obavjestenje Tip {  get; set; }
    }
}
