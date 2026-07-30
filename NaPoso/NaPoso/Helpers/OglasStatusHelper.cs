using static NaPoso.Enums.Enums;

namespace NaPoso.Helpers
{
    public static class OglasStatusHelper
    {
        /// <summary>
        /// Oglas može biti samo aktivan ili završen u korisničkom prikazu.
        /// </summary>
        public static string GetLabel(Status status) =>
            status == Status.Zavrsen ? "Završen" : "Aktivan";
    }
}
