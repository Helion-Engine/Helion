using System;

namespace Helion.Util.Configs.Options;

[AttributeUsage(AttributeTargets.Field)]
public class OptionMenuAttribute : Attribute
{
    public OptionMenuAttribute(OptionSectionType section, string name, bool disabled = false)
    {
        Section = section;
        Name = name;
        Disabled = disabled;
    }

    public readonly OptionSectionType Section;
    public readonly string Name;
    public readonly bool Disabled;
}