namespace SAT242516026.Models.Attributes;

public class TitleAttribute(string title) : Attribute
{
    public string Title { get; set; } = title;
}