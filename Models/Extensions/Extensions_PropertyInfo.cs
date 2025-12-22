using System.Reflection;
using EditableAttr = SAT242516026.Models.Attributes.EditableAttribute;
using ViewableAttr = SAT242516026.Models.Attributes.ViewableAttribute;
using SortableAttr = SAT242516026.Models.Attributes.SortableAttribute;
using SAT242516026.Models.Attributes;

namespace SAT242516026.Models.Extensions;

public static class Extensions_PropertyInfo
{
    public static bool Sortable(this PropertyInfo prop)
    {
        if (prop.GetCustomAttribute(typeof(SortableAttr)) is SortableAttr a)
            return a.Value;
        return false;
    }

    public static bool Editable(this PropertyInfo prop)
    {
        if (prop.GetCustomAttribute(typeof(EditableAttr)) is EditableAttr a)
            return a.Value;
        return false;
    }

    public static bool Viewable(this PropertyInfo prop)
    {
        if (prop.GetCustomAttribute(typeof(ViewableAttr)) is ViewableAttr a)
            return a.Value;
        return false;
    }

    public static string LocalizedDescription(this PropertyInfo prop)
    {
        try
        {
            var a = prop.GetCustomAttribute<LocalizedDescriptionAttribute>();
            return a?.Description ?? prop.Name;
        }
        catch
        {
            return prop.Name;
        }
    }
}
