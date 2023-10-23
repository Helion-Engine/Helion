using System;

namespace Helion.Util.Configs.Options;

[AttributeUsage(AttributeTargets.Field)]
public class OptionMenuAttribute : Attribute
{
    public OptionMenuAttribute(OptionSectionType section, string name, bool disabled = false, bool spacer = false)
    {
        Section = section;
        Name = name;
        Disabled = disabled;
        Spacer = spacer;
    }

    public readonly OptionSectionType Section;
    public readonly string Name;
    public readonly bool Disabled;
    public readonly bool Spacer;
}