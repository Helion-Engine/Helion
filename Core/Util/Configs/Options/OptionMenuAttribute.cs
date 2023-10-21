using System;

namespace Helion.Util.Configs.Options;

[AttributeUsage(AttributeTargets.Field)]
public class OptionMenuAttribute(OptionSectionType section, string name, bool disabled = false) : Attribute
{
    public readonly OptionSectionType Section = section;
    public readonly string Name = name;
    public readonly bool Disabled = disabled;
}