namespace NaPoso.Models
{
    public class Recenzija
    {
        public int Id { get; set; }
        public int Ocjena { get; set; }
        public string Sadrzaj { get; set; }

        public string KlijentId { get; set; }  

        public string RadnikId { get; set; }  
    }
}
