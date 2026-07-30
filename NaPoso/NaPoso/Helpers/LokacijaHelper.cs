namespace NaPoso.Helpers
{
    public static class LokacijaHelper
    {
        private static readonly Dictionary<string, string> Lokativ = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sarajevo"] = "Sarajevu",
            ["Tuzla"] = "Tuzli",
            ["Zenica"] = "Zenici",
            ["Mostar"] = "Mostaru",
            ["Banja Luka"] = "Banjoj Luci",
            ["Bihać"] = "Bihaću",
            ["Travnik"] = "Travniku",
            ["Bugojno"] = "Bugojnu",
            ["Doboj"] = "Doboju",
            ["Konjic"] = "Konjicu",
        };

        /// <summary>
        /// Vraća grad u lokativu (npr. "Sarajevo" → "Sarajevu") za korištenje uz predlog "u".
        /// </summary>
        public static string UGradu(string? grad)
        {
            if (string.IsNullOrWhiteSpace(grad))
                return "u gradu";

            var trimmed = grad.Trim();
            if (Lokativ.TryGetValue(trimmed, out var lokativ))
                return $"u {lokativ}";

            return $"u {trimmed}u";
        }
    }
}
