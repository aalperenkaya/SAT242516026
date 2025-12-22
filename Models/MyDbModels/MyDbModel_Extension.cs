using System.Reflection;
using SAT242516026.Models.Extensions;

namespace SAT242516026.Models.MyDbModels;

public static class MyDbModel_Extension
{
    public static IDictionary<object, object> GetOrderByItems<E>(this MyDbModel<E> _)
        where E : class, new()
    {
        var dict = new Dictionary<object, object>();

        foreach (var p in typeof(E).GetProperties().Where(x => x.GetIndexParameters().Length == 0))
        {
            if (!p.Sortable()) continue;

            dict.Add($"{p.LocalizedDescription()} ↑", $"{p.Name} asc");
            dict.Add($"{p.LocalizedDescription()} ↓", $"{p.Name} desc");
        }

        if (dict.Count == 0)
        {
            dict.Add("Id ↑", "Id asc");
            dict.Add("Id ↓", "Id desc");
        }

        return dict;
    }
}
