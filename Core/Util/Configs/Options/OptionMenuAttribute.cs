﻿using System;

namespace Helion.Util.Configs.Options;

[AttributeUsage(AttributeTargets.Field)]
public class OptionMenuAttribute : Attribute
{
    public OptionMenuAttribute(OptionSectionType section, string name, bool disabled = false, bool spacer = false, bool allowReset = true, DialogType dialogType = Options.DialogType.Default)
    {
        Section = section;
        Name = name;
        Disabled = disabled;
        Spacer = spacer;
        AllowBulkReset = allowReset;
        DialogType = dialogType;
    }

    public readonly OptionSectionType Section;
    public readonly string Name;
    public readonly bool Disabled;
    public readonly bool Spacer;
    public readonly bool AllowBulkReset;
    public readonly DialogType? DialogType;
}