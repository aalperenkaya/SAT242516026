using System.Reflection;
using SAT242516026.Models.Attributes;

namespace SAT242516026.Models.Extensions;

public static class Extensions_PropertyInfo
{
    public static bool IsEditable(this PropertyInfo p)
        => p.GetCustomAttribute<EditableAttribute>()?.Enabled ?? false;

    public static bool IsViewable(this PropertyInfo p)
        => p.GetCustomAttribute<ViewableAttribute>()?.Enabled ?? false;

    public static bool IsSortable(this PropertyInfo p)
        => p.GetCustomAttribute<SortableAttribute>()?.Enabled ?? false;

    public static string? LocalizedDescription(this PropertyInfo p)
        => p.GetCustomAttribute<LocalizedDescriptionAttribute>()?.Key;

    public static string LocalizedDescriptionOrName(this PropertyInfo p)
        => p.LocalizedDescription() ?? p.Name;
}
