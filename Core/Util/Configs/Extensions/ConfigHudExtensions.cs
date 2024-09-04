using Helion.Util.Configs.Components;

namespace Helion.Util.Configs.Extensions;

public static class ConfigHudExtensions
{
    public static int GetHudScaled(this ConfigHud config, int baseSize) =>
        (int)(baseSize * config.Scale.Value);

    public static int GetHudSmallFontSize(this ConfigHud config) =>
        GetHudScaled(config, 12);

    public static int GetHudMediumFontSize(this ConfigHud config) =>
        GetHudScaled(config, 16);

    public static int GetHudLargeFontSize(this ConfigHud config) =>
        GetHudScaled(config, 20);
}
