using Helion.Util.Configs.Components;

namespace Helion.Util.Configs.Extensions;

public static class ConfigHudExtensions
{
    public static int GetScaled(this ConfigHud config, int baseSize) =>
        (int)(baseSize * config.Scale.Value);

    public static int GetSmallFontSize(this ConfigHud config) =>
        GetScaled(config, 12);

    public static int GetMediumFontSize(this ConfigHud config) =>
        GetScaled(config, 16);

    public static int GetLargeFontSize(this ConfigHud config) =>
        GetScaled(config, 20);
}
