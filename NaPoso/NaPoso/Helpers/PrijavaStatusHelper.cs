using static NaPoso.Enums.Enums;

namespace NaPoso.Helpers
{
    public static class PrijavaStatusHelper
    {
        public static string GetLabel(Status status) => status switch
        {
            Status.Aktivan => "Na čekanju",
            Status.Prihvacen => "Prihvaćeno",
            Status.Placen => "Plaćeno",
            Status.Zavrsen => "Završeno",
            Status.Neaktivan => "Nije odabrano",
            _ => "Aktivno"
        };

        public static string GetBadgeClass(Status status) => status switch
        {
            Status.Aktivan => "bg-info",
            Status.Prihvacen => "bg-primary",
            Status.Placen => "bg-success",
            Status.Zavrsen => "bg-success",
            Status.Neaktivan => "bg-secondary",
            _ => "bg-secondary"
        };
    }
}
