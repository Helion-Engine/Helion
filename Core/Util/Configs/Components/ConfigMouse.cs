using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigMouse
{
    [ConfigInfo("If the player can look up and down with the mouse.", demo: true)]
    [OptionMenu(OptionSectionType.Mouse, "Mouse Look")]
    public readonly ConfigValue<bool> Look = new(true);
    
    [ConfigInfo("Forward/backward movement speed.")]
    [OptionMenu(OptionSectionType.Mouse, "Movement Speed")]
    public readonly ConfigValue<double> ForwardBackwardSpeed = new(0, GreaterOrEqual(0.0));

    [ConfigInfo("A scale for both the pitch and yaw, meaning this affects both axes.")]
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

    [ConfigInfo("If mouse input should be interpolated.")]
    [OptionMenu(OptionSectionType.Mouse, "Interpolate")]
    public readonly ConfigValue<bool> Interpolate = new(false);

    [ConfigInfo("If the mouse should be focused on the window or not.")]
    [OptionMenu(OptionSectionType.Mouse, "Focus")]
    public readonly ConfigValue<bool> Focus = new(true);

    [ConfigInfo("A scaling divisor that allows other sensitivities to be reasonable values.")]
    public readonly ConfigValue<double> PixelDivisor = new(1024.0, Greater(0.0));
}
