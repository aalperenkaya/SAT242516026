using System.Data;
using System.Reflection;

namespace SAT242516026.Models.Extensions;

public static class Exttensions_DataTable
{
    public static IEnumerable<T> DataTableToList<T>(this DataTable dt) where T : class, new()
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        foreach (DataRow row in dt.Rows)
        {
            var obj = new T();

            foreach (var p in props)
            {
                if (!dt.Columns.Contains(p.Name)) continue;

                var val = row[p.Name];
                if (val == DBNull.Value) continue;

                var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                p.SetValue(obj, Convert.ChangeType(val, t));
            }

            yield return obj;
        }
    }
}
