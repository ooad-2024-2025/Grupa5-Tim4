namespace NaPoso.Models
{
    public class DokumentiKorisnika
    {
        public int Id { get; set; }
        public string FileName { get; set; } 
        public Korisnik Korisnik { get; set; } 
    }
}
