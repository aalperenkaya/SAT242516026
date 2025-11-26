namespace SAT242516026.Models.Attributes;

public class ColorAttribute(string color) : Attribute
{
    public string Color { get; set; } = color;
}