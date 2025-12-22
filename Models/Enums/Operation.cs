using SAT242516026.Models.Attributes;

namespace SAT242516026.Models.Enums;

public enum Operations
{
    [Title("Liste")][Color("success")] List,
    [Title("Ekle")][Color("primary")] Add,
    [Title("Güncelle")][Color("warning")] Update,
    [Title("Sil")][Color("danger")] Remove,
    [Title("Detay")][Color("info")] Detail,
    [Title("İptal")][Color("secondary")] Cancel,
    [Title("Sıfırla")][Color("secondary")] Reset
}
