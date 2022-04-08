using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigMouse
{
    [ConfigInfo("If the mouse should be focused on the window or not.")]
    public readonly ConfigValue<bool> Focus = new(true);

    [ConfigInfo("If we should be able to look around the level with the mouse.")]
    public readonly ConfigValue<bool> Look = new(true);

    [ConfigInfo("The vertical sensitivity. This is multiplied by the sensitivity value for a final calculation.")]
    public readonly ConfigValue<double> Pitch = new(1.0);

    [ConfigInfo("A scaling divisor that allows other sensitivities to be reasonable values.")]
    public readonly ConfigValue<double> PixelDivisor = new(1024.0, Greater(0.0));

    [ConfigInfo("If the mouse should use raw input.")]
    public readonly ConfigValue<bool> RawInput = new(true);

    [ConfigInfo("A scale for both the pitch and yaw, meaning this affects both axes.")]
    public readonly ConfigValue<double> Sensitivity = new(1.0);

    [ConfigInfo("Forward/backward movement speed.")]
    public readonly ConfigValue<double> ForwardBackwardSpeed = new(0, GreaterOrEqual(0.0));

    [ConfigInfo("The horizontal sensitivity. This is multiplied by the sensitivity value for a final calculation.")]
    public readonly ConfigValue<double> Yaw = new(1.0);
}
