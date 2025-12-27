namespace SAT242516026.Models.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SortableAttribute : Attribute
{
    public bool Enabled { get; }
    public SortableAttribute(bool enabled) => Enabled = enabled;
}
