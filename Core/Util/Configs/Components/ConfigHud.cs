using Helion.Geometry.Vectors;
using Helion.Geometry;
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

    [ConfigInfo("Overlay automap over game window.")]
    [OptionMenu(OptionSectionType.Automap, "Overlay")]
    public readonly ConfigValue<bool> Overlay = new(false);

    [ConfigInfo("Automap rotates with the player so the top is forward.")]
    [OptionMenu(OptionSectionType.Automap, "Rotate")]
    public readonly ConfigValue<bool> Rotate = new(false);

    [ConfigInfo("Background color for the default automap.")]
    [OptionMenu(OptionSectionType.Automap, "Background color")]
    public readonly ConfigValue<Vec3I> BackgroundColor = new((0, 0, 0), ClampColor);

    [ConfigInfo("Backdrop color for the overlay automap.")]
    [OptionMenu(OptionSectionType.Automap, "Backdrop color")]
    public readonly ConfigValue<Vec3I> OverlayBackdropColor = new((0, 0, 0), ClampColor);

    [ConfigInfo("Background color transparency when using overlay.")]
    [OptionMenu(OptionSectionType.Automap, "Backdrop transparency")]
    public readonly ConfigValue<double> OverlayBackdropTransparency = new(0.7, ClampNormalized);

    [ConfigInfo("Shows map title on the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Show map title")]
    public readonly ConfigValue<bool> MapTitle = new(true);

    public AutomapLineColors DefaultColors = new(false);
    public AutomapLineColors OverlayColors = new(true);
}

public class AutomapLineColors(bool overlay)
{
    [ConfigInfo("", save: false, legacy: true)]
    [OptionMenu(OptionSectionType.Automap, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader Header = new(overlay ? "Overlay Colors" : "Default Colors");

    [ConfigInfo("One-sided wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Wall color")]
    public readonly ConfigValue<Vec3I> WallColor = new(overlay ? (0, 0xFF, 0) : (0xFF, 0xFF, 0xFF), ClampColor);

    [ConfigInfo("One-sided wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Two-sided wall color")]
    public readonly ConfigValue<Vec3I> TwoSidedWallColor = new(overlay ? (0, 0x80, 0) : (0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Unseen wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Unseen wall color")]
    public readonly ConfigValue<Vec3I> UnseenWallColor = new(overlay ? (0, 0x80, 0) : (0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Teleport line color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Teleport line color")]
    public readonly ConfigValue<Vec3I> TeleportLineColor = new(overlay ? (0xFF, 0x00, 0xFF) : (0x00, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Player color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Player color")]
    public readonly ConfigValue<Vec3I> PlayerColor = new(overlay ? (0xFF, 0xFF, 0xFF) : (0x00, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Thing color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Thing color")]
    public readonly ConfigValue<Vec3I> ThingColor = new((0xFF, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Pickup thing color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Pickup color")]
    public readonly ConfigValue<Vec3I> PickupColor = new(overlay ? (0x00, 0x00, 0xFF) : (0x00, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Monster color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Monster color")]
    public readonly ConfigValue<Vec3I> MonsterColor = new((0xFF, 0x00, 0x00), ClampColor);

    [ConfigInfo("Dead monster color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Dead monster color")]
    public readonly ConfigValue<Vec3I> DeadMonsterColor = new((0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Marker color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Marker color")]
    public readonly ConfigValue<Vec3I> MakerColor = new((0x80, 0x00, 0x80), ClampColor);

    [ConfigInfo("Alt marker color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Marker color alt")]
    public readonly ConfigValue<Vec3I> AltMakerColor = new((0xAD, 0xD8, 0xE6), ClampColor);
}

public class ConfigHud
{
    [ConfigInfo("Shows crosshair.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair enabled")]
    public readonly ConfigValue<bool> Crosshair = new(true);

    [ConfigInfo("Crosshair type.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair")]
    public readonly ConfigValue<CrosshairStyle> CrosshairType = new(CrosshairStyle.Cross1);

    [ConfigInfo("Crosshair color.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair color")]
    public readonly ConfigValue<CrossColor> CrosshairColor = new(CrossColor.Green);

    [ConfigInfo("Crosshair target color.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair target color")]
    public readonly ConfigValue<CrossColor> CrosshairTargetColor = new(CrossColor.Red);

    [ConfigInfo("Crosshair transparency.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair transparency")]
    public readonly ConfigValue<double> CrosshairTransparency = new(0.5, ClampNormalized);

    [ConfigInfo("Crosshair scale.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair scale")]
    public readonly ConfigValue<double> CrosshairScale = new(1.0);

    [ConfigInfo("The amount of view bobbing. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "View bob", spacer: true)]
    public readonly ConfigValue<double> ViewBob = new(1.0, ClampNormalized);

    [ConfigInfo("The amount of weapon bobbing. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "Weapon bob")]
    public readonly ConfigValue<double> WeaponBob = new(1.0, ClampNormalized);

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

    [ConfigInfo("Amount of hud transparency.")]
    [OptionMenu(OptionSectionType.Hud, "Hud transparency")]
    public readonly ConfigValue<double> Transparency = new(0.0, ClampNormalized);

    [ConfigInfo("Max hud messages.")]
    [OptionMenu(OptionSectionType.Hud, "Max hud messages")]
    public readonly ConfigValue<int> MaxMessages = new(4, GreaterOrEqual(0));

    [ConfigInfo("Horizontal hud margin percentage.")]
    [OptionMenu(OptionSectionType.Hud, "Horizontal margin percent (0.0 - 1.0)")]
    public readonly ConfigValue<double> HorizontalMargin = new(0, ClampNormalized);

    public readonly ConfigHudAutoMap AutoMap = new();

    // Legacy stuff
    [ConfigInfo("The amount of view and weapon bobbing. 0.0 is off, 1.0 is normal.", legacy: true)]
    public readonly ConfigValue<double> MoveBob = new(1.0, ClampNormalized);
}
