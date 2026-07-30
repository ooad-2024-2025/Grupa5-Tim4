using System.ComponentModel.DataAnnotations;

namespace NaPoso.Models
{
    public class Statistika
    {
        [Display(Name = "Broj korisnika")]
        public int BrojKorisnika { get; set; }

        [Display(Name = "Broj poslova")]
        public int BrojPoslova { get; set; }

        [Display(Name = "Broj klijenata")]
        public int BrojKlijenata { get; set; }

        [Display(Name = "Broj radnika")]
        public int BrojRadnika { get; set; }

        [Display(Name = "Završeni poslovi")]
        public int BrojZavrsenihPoslova { get; set; }

        [Display(Name = "Plaćeni poslovi")]
        public int PlaceniPoslovi { get; set; }

        [Display(Name = "Aktivni poslovi")]
        public int AktivniPoslovi { get; set; }

        [Display(Name = "Prosječna ocjena")]
        public double ProsjecnaOcjena { get; set; }
    }
}
