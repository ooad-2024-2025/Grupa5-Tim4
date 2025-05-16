using static NaPoso.Enums.Enums;

namespace NaPoso.Models
{
    public class Oglas
    {
        public int Id { get; set; }
        public int KlijentId { get; set; }
        public int RadnikId { get; set; }
        public string Opis { get; set; }
        public string Lokacija { get; set; }
        public string TipPosla {  get; set; }
        public double CijenaPosla { get; set; }
        public string Naslov {  get; set; }
        public Recenzija Recenzija {  get; set; }  
        public Status Status { get; set; }
    }
}
