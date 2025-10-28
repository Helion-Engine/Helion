using System;

namespace Helion.Util.Configs.Options;

[AttributeUsage(AttributeTargets.Field)]
public class OptionMenuAttribute(OptionSectionType section, string name, bool disabled = false, bool spacer = false,
    bool allowReset = true, bool windowsPlatform = false, DialogType dialogType = DialogType.Default,
    double sliderMin = 0, double sliderMax = 1, double sliderStep = 0) : Attribute
{
    public readonly OptionSectionType Section = section;
    public readonly string Name = name;
    public readonly bool Disabled = disabled;
    public readonly bool Spacer = spacer;
    public readonly bool AllowBulkReset = allowReset;
    public readonly DialogType? DialogType = dialogType;
    public readonly double SliderMin = sliderMin;
    public readonly double SliderMax = sliderMax;
    public readonly double SliderStep = sliderStep;
    public readonly bool WindowsPlatform = windowsPlatform;
}