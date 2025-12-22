using System.ComponentModel;
using System.Resources;

namespace SAT242516026.Models.Attributes;

public class LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
    : DescriptionAttribute
{
    private readonly ResourceManager _resource = new(resourceType);

    public override string Description
    {
        get
        {
            var desc = _resource.GetString(resourceKey);
            return string.IsNullOrEmpty(desc) ? resourceKey : desc;
        }
    }
}



