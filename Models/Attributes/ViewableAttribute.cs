namespace SAT242516026.Models.Attributes;

public class ViewableAttribute(bool value) : Attribute
{
    public bool Value { get; set; } = value;
}
