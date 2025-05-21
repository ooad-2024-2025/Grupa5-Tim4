namespace NaPoso.Models
{
    public class ObavijestKorisniku
    {
        public int Id { get; set; }
        public string KorisnikId { get; set; }
        public Obavijest obavijest { get; set; }
      
    }
}
