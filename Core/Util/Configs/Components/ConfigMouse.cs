using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigMouse: ConfigElement<ConfigMouse>
{
    [ConfigInfo("Player can look up and down with the mouse.", demo: true)]
    [OptionMenu(OptionSectionType.Mouse, "Mouse Look")]
    public readonly ConfigValue<bool> Look = new(true);
    
    [ConfigInfo("Forward/backward movement speed when mouse look is disabled.")]
    [OptionMenu(OptionSectionType.Mouse, "Movement Speed")]
    public readonly ConfigValue<double> ForwardBackwardSpeed = new(0, GreaterOrEqual(0.0));

    [ConfigInfo("Scale for both the pitch and yaw (affects both axes).")]
    [OptionMenu(OptionSectionType.Mouse, "Overall Sensitivity", spacer: true)]
    public readonly ConfigValue<double> Sensitivity = new(1.0);

    [ConfigInfo("Vertical sensitivity. This is multiplied by the overall sensitivity value.")]
    [OptionMenu(OptionSectionType.Mouse, "Vertical Sensitivity")]
    public readonly ConfigValue<double> Pitch = new(1.0);

    [ConfigInfo("Horizontal sensitivity. This is multiplied by the overall sensitivity value.")]
    [OptionMenu(OptionSectionType.Mouse, "Horizontal Sensitivity")]
    public readonly ConfigValue<double> Yaw = new(1.0);
    
    [ConfigInfo("Invert mouse Y.")]
    [OptionMenu(OptionSectionType.Mouse, "Invert Y", spacer: true)]
    public readonly ConfigValue<bool> InvertY = new(false);

    [ConfigInfo("Interpolate mouse input.")]
    [OptionMenu(OptionSectionType.Mouse, "Interpolate")]
    public readonly ConfigValue<bool> Interpolate = new(false);

    [ConfigInfo("Application window steals mouse focus when active.")]
    [OptionMenu(OptionSectionType.Mouse, "Focus")]
    public readonly ConfigValue<bool> Focus = new(true);

    [ConfigInfo("Scaling divisor that allows other sensitivities to be reasonable values.")]
    public readonly ConfigValue<double> PixelDivisor = new(1024.0, Greater(0.0));
}
