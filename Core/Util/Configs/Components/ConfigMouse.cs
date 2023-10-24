using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigMouse
{
    [ConfigInfo("If we should be able to look around the level with the mouse.", demo: true)]
    [OptionMenu(OptionSectionType.Mouse, "Mouse look")]
    public readonly ConfigValue<bool> Look = new(true);
    
    [ConfigInfo("Forward/backward movement speed.")]
    [OptionMenu(OptionSectionType.Mouse, "Movement speed")]
    public readonly ConfigValue<double> ForwardBackwardSpeed = new(0, GreaterOrEqual(0.0));

    [ConfigInfo("A scale for both the pitch and yaw, meaning this affects both axes.")]
    [OptionMenu(OptionSectionType.Mouse, "Sensitivity", spacer: true, scale: 5)]
    public readonly ConfigValue<double> Sensitivity = new(1.0);

    [ConfigInfo("The vertical sensitivity. This is multiplied by the sensitivity value for a final calculation.")]
    [OptionMenu(OptionSectionType.Mouse, "Vertical Sensitivity")]
    public readonly ConfigValue<double> Pitch = new(1.0);

    [ConfigInfo("The horizontal sensitivity. This is multiplied by the sensitivity value for a final calculation.")]
    [OptionMenu(OptionSectionType.Mouse, "Horizontal Sensitivity")]
    public readonly ConfigValue<double> Yaw = new(1.0);

    [ConfigInfo("If the mouse should interpolate.")]
    [OptionMenu(OptionSectionType.Mouse, "Interpolate", spacer: true)]
    public readonly ConfigValue<bool> Interpolate = new(true);

    [ConfigInfo("If the mouse should be focused on the window or not.")]
    [OptionMenu(OptionSectionType.Mouse, "Focus")]
    public readonly ConfigValue<bool> Focus = new(true);

    [ConfigInfo("A scaling divisor that allows other sensitivities to be reasonable values.")]
    [OptionMenu(OptionSectionType.Mouse, "Pixel divisor", disabled: true)]
    public readonly ConfigValue<double> PixelDivisor = new(1024.0, Greater(0.0));
}
