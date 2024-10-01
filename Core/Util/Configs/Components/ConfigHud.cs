using Helion.Geometry;
using Helion.Geometry.Vectors;
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
    [ConfigInfo("Overlay automap over game window.")]
    [OptionMenu(OptionSectionType.Automap, "Overlay")]
    public readonly ConfigValue<bool> Overlay = new(true);

    [ConfigInfo("Automap rotates with the player so the top is forward.")]
    [OptionMenu(OptionSectionType.Automap, "Rotate")]
    public readonly ConfigValue<bool> Rotate = new(true);

    [ConfigInfo("Background color for the default automap.")]
    [OptionMenu(OptionSectionType.Automap, "Background Color")]
    public readonly ConfigValue<Vec3I> BackgroundColor = new((0, 0, 0), ClampColor);

    [ConfigInfo("Backdrop color for the overlay automap.")]
    [OptionMenu(OptionSectionType.Automap, "Backdrop Color")]
    public readonly ConfigValue<Vec3I> OverlayBackdropColor = new((0, 0, 0), ClampColor);

    [ConfigInfo("Background color transparency when using overlay.")]
    [OptionMenu(OptionSectionType.Automap, "Backdrop Transparency")]
    public readonly ConfigValue<double> OverlayBackdropTransparency = new(0.7, ClampNormalized);

    [ConfigInfo("Show map title on the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Show Map Title")]
    public readonly ConfigValue<bool> MapTitle = new(true);

    // Internal to the client
    [ConfigInfo("Amount to scale automap.", save: false)]
    public readonly ConfigValue<double> Scale = new(1.0);

    public AutomapLineColors DefaultColors = new(false);
    public AutomapLineColors OverlayColors = new(true);
}

public class AutomapLineColors(bool overlay)
{
    [ConfigInfo("", save: false, legacy: true)]
    [OptionMenu(OptionSectionType.Automap, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader Header = new(overlay ? "Overlay Colors" : "Default Colors");

    [ConfigInfo("One-sided wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Wall Color")]
    public readonly ConfigValue<Vec3I> WallColor = new(overlay ? (0, 0xFF, 0) : (0xFF, 0xFF, 0xFF), ClampColor);

    [ConfigInfo("One-sided wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Two-sided Wall Color")]
    public readonly ConfigValue<Vec3I> TwoSidedWallColor = new(overlay ? (0, 0x80, 0) : (0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Unseen wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Unseen Wall Color")]
    public readonly ConfigValue<Vec3I> UnseenWallColor = new(overlay ? (0, 0x80, 0) : (0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Teleport line color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Teleport Line Color")]
    public readonly ConfigValue<Vec3I> TeleportLineColor = new(overlay ? (0xFF, 0x00, 0xFF) : (0x00, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Player color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Player Color")]
    public readonly ConfigValue<Vec3I> PlayerColor = new(overlay ? (0xFF, 0xFF, 0xFF) : (0x00, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Thing color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Thing Color")]
    public readonly ConfigValue<Vec3I> ThingColor = new((0xFF, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Pickup thing color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Pickup Color")]
    public readonly ConfigValue<Vec3I> PickupColor = new(overlay ? (0x00, 0x00, 0xFF) : (0x00, 0xFF, 0x00), ClampColor);

    [ConfigInfo("Monster color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Monster Color")]
    public readonly ConfigValue<Vec3I> MonsterColor = new((0xFF, 0x00, 0x00), ClampColor);

    [ConfigInfo("Dead monster color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Dead Monster Color")]
    public readonly ConfigValue<Vec3I> DeadMonsterColor = new((0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Marker color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Marker Color")]
    public readonly ConfigValue<Vec3I> MakerColor = new((0x80, 0x00, 0x80), ClampColor);

    [ConfigInfo("Alt marker color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Marker Color Alt")]
    public readonly ConfigValue<Vec3I> AltMakerColor = new((0xAD, 0xD8, 0xE6), ClampColor);
}

public class ConfigHud
{
    // Crosshair

    [ConfigInfo("Shows crosshair.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Enabled")]
    public readonly ConfigValue<bool> Crosshair = new(true);

    [ConfigInfo("Crosshair type.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair")]
    public readonly ConfigValue<CrosshairStyle> CrosshairType = new(CrosshairStyle.Cross1);

    [ConfigInfo("Crosshair color.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Color")]
    public readonly ConfigValue<CrossColor> CrosshairColor = new(CrossColor.Green);

    [ConfigInfo("Crosshair target color.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Target Color")]
    public readonly ConfigValue<CrossColor> CrosshairTargetColor = new(CrossColor.Red);

    [ConfigInfo("Use crosshair as health indicator.  Crosshair gets redder as player loses health.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Health Indicator")]
    public readonly ConfigValue<bool> CrosshairHealthIndicator = new(false);

    [ConfigInfo("Crosshair transparency.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Transparency")]
    public readonly ConfigValue<double> CrosshairTransparency = new(0.5, ClampNormalized);

    [ConfigInfo("Crosshair scale.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Scale")]
    public readonly ConfigValue<double> CrosshairScale = new(1.0);


    // Bobbin'

    [ConfigInfo("Amount of view bobbing. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "View Bob", spacer: true)]
    public readonly ConfigValue<double> ViewBob = new(1.0, ClampNormalized);

    [ConfigInfo("Amount of weapon bobbing. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "Weapon Bob")]
    public readonly ConfigValue<double> WeaponBob = new(1.0, ClampNormalized);


    // Status bar

    [ConfigInfo("Size of the status bar.")]
    [OptionMenu(OptionSectionType.Hud, "Status Bar Size", spacer: true)]
    public readonly ConfigValue<StatusBarSizeType> StatusBarSize = new(StatusBarSizeType.Minimal, OnlyValidEnums<StatusBarSizeType>());

    [ConfigInfo("Background texture for status bar when it doesn't fill the screen.")]
    [OptionMenu(OptionSectionType.Hud, "Status Bar Texture", dialogType: DialogType.TexturePicker)]
    public readonly ConfigValue<string> BackgroundTexture = new("GRNROCK");


    // Formatting, scaling

    [ConfigInfo("Automatically scale HUD.")]
    [OptionMenu(OptionSectionType.Hud, "Autoscale HUD", spacer: true)]
    public readonly ConfigValue<bool> AutoScale = new(true);

    [ConfigInfo("Amount to scale the HUD.")]
    [OptionMenu(OptionSectionType.Hud, "HUD Scale")]
    public readonly ConfigValue<double> Scale = new(2.0, Greater(0.0));

    [ConfigInfo("Amount of HUD transparency.")]
    [OptionMenu(OptionSectionType.Hud, "HUD Transparency")]
    public readonly ConfigValue<double> Transparency = new(0.0, ClampNormalized);

    [ConfigInfo("Max HUD messages.")]
    [OptionMenu(OptionSectionType.Hud, "Max HUD Messages")]
    public readonly ConfigValue<int> MaxMessages = new(4, GreaterOrEqual(0));

    [ConfigInfo("Horizontal HUD margin percentage  (0.0 - 1.0).")]
    [OptionMenu(OptionSectionType.Hud, "Horizontal Margin Percent")]
    public readonly ConfigValue<double> HorizontalMargin = new(0, ClampNormalized);

    [ConfigInfo("Font upscaling ratio (1 - 5)")]
    [OptionMenu(OptionSectionType.Hud, "Font Upscale Ratio")]
    public readonly ConfigValue<int> FontUpscaleRatio = new(4, Clamp(1, 5));

    // Stats and diagnostics

    [ConfigInfo("Render average frames per second in corner of display.")]
    [OptionMenu(OptionSectionType.Hud, "Show FPS", spacer: true)]
    public readonly ConfigValue<bool> ShowFPS = new(false);

    [ConfigInfo("Render min/max frames per second in corner of display.")]
    [OptionMenu(OptionSectionType.Hud, "Show Min/Max FPS")]
    public readonly ConfigValue<bool> ShowMinMaxFPS = new(false);

    [ConfigInfo("Render world statistics (kills, secrets, items, time) in corner of display.")]
    [OptionMenu(OptionSectionType.Hud, "Show World Stats")]
    public readonly ConfigValue<bool> ShowStats = new(false);

    public readonly ConfigHudAutoMap AutoMap = new();

    // Legacy stuff
    [ConfigInfo("Amount of view and weapon bobbing. 0.0 is off, 1.0 is normal.", legacy: true)]
    public readonly ConfigValue<double> MoveBob = new(1.0, ClampNormalized);
}
