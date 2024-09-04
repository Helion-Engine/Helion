using Helion.Util.Configs.Components;

namespace Helion.Util.Configs.Extensions;

public static class ConfigMenuExtensions
{
    public static int GetMenuScaled(this ConfigWindow config, int baseSize) =>
        (int)(baseSize * config.MenuScale.Value);

    public static int GetMenuSmallFontSize(this ConfigWindow config) =>
        GetMenuScaled(config, 12);

    public static int GetMenuMediumFontSize(this ConfigWindow config) =>
        GetMenuScaled(config, 16);

    public static int GetMenuLargeFontSize(this ConfigWindow config) =>
        GetMenuScaled(config, 20);
}
