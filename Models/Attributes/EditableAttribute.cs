namespace SAT242516026.Models.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EditableAttribute : Attribute
{
    public bool Enabled { get; }
    public EditableAttribute(bool enabled) => Enabled = enabled;
}
