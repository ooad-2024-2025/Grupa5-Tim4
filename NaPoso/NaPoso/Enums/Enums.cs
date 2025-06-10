using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NaPoso.Enums
{
    public class Enums
    {
        public enum Uloge
        {
            Administrator,
            Klijent,
            Radnik
        }

        public enum Status
        {
            Aktivan = 1,
            Neaktivan = 0,
            Prihvacen = 2,
            Odbijen = 3,
            Placen = 4

        }

        public enum Obavjestenje
        {
            Email,
            DrugaObavjestenja
        }
    }
}
