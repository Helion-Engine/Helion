using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.World.StatusBar;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigHudAutoMap
{
    [ConfigInfo("Amount to scale automap.", save: false)]
    [OptionMenu(OptionSectionType.Hud, "Automap scale")]
    public readonly ConfigValue<double> Scale = new(1.0);
}

public class ConfigHud
{
    public readonly ConfigHudAutoMap AutoMap = new();

    [ConfigInfo("The amount of move bobbing the weapon does. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "Move bob")]
    public readonly ConfigValue<double> MoveBob = new(1.0, ClampNormalized);

    [ConfigInfo("Amount to scale minimal hud.")]
    [OptionMenu(OptionSectionType.Hud, "Hud scale")]
    public readonly ConfigValue<double> Scale = new(2.0, Greater(0.0));

    [ConfigInfo("The size of the status bar.")]
    [OptionMenu(OptionSectionType.Hud, "Status bar size")]
    public readonly ConfigValue<StatusBarSizeType> StatusBarSize = new(StatusBarSizeType.Minimal, OnlyValidEnums<StatusBarSizeType>());

    [ConfigInfo("If average frames per second should be rendered.")]
    [OptionMenu(OptionSectionType.Hud, "Show FPS")]
    public readonly ConfigValue<bool> ShowFPS = new(false);

    [ConfigInfo("If min/max frames per second should be rendered.")]
    public readonly ConfigValue<bool> ShowMinMaxFPS = new(false);

    [ConfigInfo("If the world stats should be rendered.")]
    [OptionMenu(OptionSectionType.Hud, "Show world stats")]
    public readonly ConfigValue<bool> ShowStats = new(false);

    [ConfigInfo("Background texture for status bar when it doesn't fill the screen.")]
    [OptionMenu(OptionSectionType.Hud, "Status bar texture")]
    public readonly ConfigValue<string> BackgroundTexture = new("W94_1");

    [ConfigInfo("Shows crosshair.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair")]
    public readonly ConfigValue<bool> Crosshair = new(true);
}
