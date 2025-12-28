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
            if (!p.IsSortable()) continue;

            var label = p.LocalizedDescriptionOrName();
            dict.Add($"{label} ↑", $"{p.Name} asc");
            dict.Add($"{label} ↓", $"{p.Name} desc");
        }

        if (dict.Count == 0)
        {
            dict.Add("Id ↑", "Id asc");
            dict.Add("Id ↓", "Id desc");
        }

        return dict;    
    }
}
