namespace SAT242516026.Models.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ViewableAttribute : Attribute
{
    public bool Enabled { get; }
    public ViewableAttribute(bool enabled) => Enabled = enabled;
}
