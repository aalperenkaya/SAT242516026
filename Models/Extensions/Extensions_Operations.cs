using SAT242516026.Models.Enums;

namespace SAT242516026.Models.Extensions;

public static class OperationsExtensions
{
    public static string Color(this Operations op) => op switch
    {
        Operations.Add => "success",
        Operations.Update => "primary",
        Operations.Remove => "danger",
        Operations.Detail => "info",
        Operations.Cancel => "secondary",
        Operations.Reset => "warning",
        Operations.List => "dark",
        _ => "secondary"
    };

    public static string Description(this Operations op) => op switch
    {
        Operations.Add => "Ekle",
        Operations.Update => "Güncelle",
        Operations.Remove => "Sil",
        Operations.Detail => "Detay",
        Operations.Cancel => "İptal",
        Operations.Reset => "Sıfırla",
        Operations.List => "Liste",
        _ => op.ToString()
    };
}
