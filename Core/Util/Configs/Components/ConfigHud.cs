using Helion.Geometry.Vectors;
using Helion.Geometry;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.World.StatusBar;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public static class HudView
{
    public const int FullSizeHudOffsetY = 16;

    public static Vec2I GetViewPortOffset(StatusBarSizeType statusBarSize, Dimension viewport)
    {
        if (statusBarSize == StatusBarSizeType.Full)
            return (0, (int)(viewport.Height / 200.0 * FullSizeHudOffsetY));
        return (0, 0);
    }
}

public class ConfigHudAutoMap
{
    // Internal to the client
    [ConfigInfo("Amount to scale automap.", save: false)]
    public readonly ConfigValue<double> Scale = new(1.0);
}

public class ConfigHud
{
    [ConfigInfo("Shows crosshair.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair")]
    public readonly ConfigValue<bool> Crosshair = new(true);

    [ConfigInfo("The amount of move bobbing the weapon does. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "Move bob")]
    public readonly ConfigValue<double> MoveBob = new(1.0, ClampNormalized);

    [ConfigInfo("The size of the status bar.")]
    [OptionMenu(OptionSectionType.Hud, "Status bar size", spacer: true)]
    public readonly ConfigValue<StatusBarSizeType> StatusBarSize = new(StatusBarSizeType.Minimal, OnlyValidEnums<StatusBarSizeType>());

    [ConfigInfo("Background texture for status bar when it doesn't fill the screen.")]
    [OptionMenu(OptionSectionType.Hud, "Status bar texture")]
    public readonly ConfigValue<string> BackgroundTexture = new("W94_1");

    [ConfigInfo("If average frames per second should be rendered.")]
    [OptionMenu(OptionSectionType.Hud, "Show FPS", spacer: true)]
    public readonly ConfigValue<bool> ShowFPS = new(false);

    [ConfigInfo("If min/max frames per second should be rendered.")]
    [OptionMenu(OptionSectionType.Hud, "Show Min/Max FPS")]
    public readonly ConfigValue<bool> ShowMinMaxFPS = new(false);

    [ConfigInfo("If the world stats should be rendered.")]
    [OptionMenu(OptionSectionType.Hud, "Show world stats")]
    public readonly ConfigValue<bool> ShowStats = new(false);

    [ConfigInfo("If the hud should be autoscaled.")]
    [OptionMenu(OptionSectionType.Hud, "Autoscale hud", spacer: true)]
    public readonly ConfigValue<bool> AutoScale = new(true);

    [ConfigInfo("Amount to scale the hud.")]
    [OptionMenu(OptionSectionType.Hud, "Hud scale")]
    public readonly ConfigValue<double> Scale = new(2.0, Greater(0.0));

    [ConfigInfo("Horizontal hud margin percentage.")]
    [OptionMenu(OptionSectionType.Hud, "Horizontal margin percent (0.0 - 1.0)")]
    public readonly ConfigValue<double> HorizontalMargin = new(0, ClampNormalized);


    public readonly ConfigHudAutoMap AutoMap = new();

}
