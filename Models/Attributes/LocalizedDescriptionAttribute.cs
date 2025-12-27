namespace SAT242516026.Models.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LocalizedDescriptionAttribute : Attribute
{
    public string Key { get; }
    public LocalizedDescriptionAttribute(string key) => Key = key;
}
