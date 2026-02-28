using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.StatusBar;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.World.StatusBar;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public static class HudView
{
    public static int GetWeaponOffset(StatusBarLayoutDef? activeStatusBar)
    {
        return GetStatusBarHeight(activeStatusBar) / 2;
    }

    public static Vec2I GetViewPortOffset(StatusBarLayoutDef? activeStatusBar, Dimension viewport)
    {
        var statusBarHeight = GetStatusBarHeight(activeStatusBar);
        return statusBarHeight > 0 ? (0, (int)(viewport.Height / 200.0 * (statusBarHeight / 2.0))) : Vec2I.Zero;
    }

    private static int GetStatusBarHeight(StatusBarLayoutDef? activeStatusBar)
    {
        if (activeStatusBar == null)
            return 0;

        return activeStatusBar.FullscreenRender ? 0 : activeStatusBar.Height;
    }
}

public class ConfigHudAutoMap: ConfigElement<ConfigHudAutoMap>
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
    [OptionMenu(OptionSectionType.Automap, "Backdrop Transparency", sliderMin: 0, sliderMax: 1.0, sliderStep: .05)]
    public readonly ConfigValue<double> OverlayBackdropTransparency = new(0.7, ClampNormalized);

    [ConfigInfo("Show map title on the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Show Map Title")]
    public readonly ConfigValue<bool> MapTitle = new(true);

    [ConfigInfo("Show stats (kills, items, secrets, time) on the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Show Stats")]
    public readonly ConfigValue<bool> ShowStats = new(true);
    
    [ConfigInfo("Use average color from key icon image.")]
    [OptionMenu(OptionSectionType.Automap, "Key Image Color")]
    public readonly ConfigValue<bool> ImageKeyColor = new(true);

    // Internal to the client
    [ConfigInfo("Amount to scale automap.", save: false)]
    public readonly ConfigValue<double> Scale = new(1.0);

    public AutomapLineColors DefaultColors = new(false);
    public AutomapLineColors OverlayColors = new(true);
}

public class AutomapLineColors(bool overlay): ConfigElement<AutomapLineColors>
{
    [ConfigInfo("", save: false, legacy: true)]
    [OptionMenu(OptionSectionType.Automap, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader Header = new(overlay ? "Overlay Colors" : "Default Colors");

    [ConfigInfo("One-sided wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Wall Color")]
    public readonly ConfigValue<Vec3I> WallColor = new(overlay ? (0, 0xFF, 0) : (0xFF, 0xFF, 0xFF), ClampColor);

    [ConfigInfo("Two-sided wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Two-sided Wall Color")]
    public readonly ConfigValue<Vec3I> TwoSidedWallColor = new(overlay ? (0, 0x80, 0) : (0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Unseen wall color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Unseen Wall Color")]
    public readonly ConfigValue<Vec3I> UnseenWallColor = new(overlay ? (0, 0x80, 0) : (0x80, 0x80, 0x80), ClampColor);

    [ConfigInfo("Teleport line color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Teleport Line Color")]
    public readonly ConfigValue<Vec3I> TeleportLineColor = new(overlay ? (0xFF, 0x00, 0xFF) : (0x00, 0xFF, 0x00), ClampColor);
    
    [ConfigInfo("Exit line color for the automap.")]
    [OptionMenu(OptionSectionType.Automap, "Exit Line Color")]
    public readonly ConfigValue<Vec3I> ExitLineColor = new((0xFF, 0x00, 0x00), ClampColor);

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

public class ConfigHud: ConfigElement<ConfigHud>
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

    [ConfigInfo("Crosshair shinks on target.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Target Shrink")]
    public readonly ConfigValue<bool> CrosshairTargetShrink = new(true);

    [ConfigInfo("Use crosshair as health indicator.  Crosshair gets redder as player loses health.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Health Indicator")]
    public readonly ConfigValue<bool> CrosshairHealthIndicator = new(false);

    [ConfigInfo("Crosshair transparency.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Transparency", sliderMin: 0, sliderMax: 1.0, sliderStep: .05)]
    public readonly ConfigValue<double> CrosshairTransparency = new(0.5, ClampNormalized);

    [ConfigInfo("Crosshair scale.")]
    [OptionMenu(OptionSectionType.Hud, "Crosshair Scale", sliderMin: 0, sliderMax: 5.0, sliderStep: .1)]
    public readonly ConfigValue<double> CrosshairScale = new(1.0);


    // Bobbin'

    [ConfigInfo("Amount of view bobbing. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "View Bob", spacer: true, sliderMin: 0, sliderMax: 1.0, sliderStep: .05)]
    public readonly ConfigValue<double> ViewBob = new(1.0, ClampNormalized);

    [ConfigInfo("Amount of weapon bobbing. 0.0 is off, 1.0 is normal.")]
    [OptionMenu(OptionSectionType.Hud, "Weapon Bob", sliderMin: 0, sliderMax: 1.0, sliderStep: .05)]
    public readonly ConfigValue<double> WeaponBob = new(1.0, ClampNormalized);


    // Status bar

    [ConfigInfo("Name of the status bar layout to use (from SBARDEF).")]
    [OptionMenu(OptionSectionType.Hud, "Status Bar Layout", spacer: true, isDynamicStringCycle: true)]
    public readonly ConfigValue<string> StatusBarLayout = new("");

    [ConfigInfo("Selects the active SBARDEF layout index.")]
    public readonly ConfigValue<int> SbarHudMode = new(0, GreaterOrEqual(0));

    [ConfigInfo("Size of the status bar.", legacy: true)]
    public readonly ConfigValue<StatusBarSizeType> StatusBarSize = new(StatusBarSizeType.Minimal);

    [ConfigInfo("Background texture for status bar when it doesn't fill the screen.")]
    [OptionMenu(OptionSectionType.Hud, "Status Bar Texture", dialogType: DialogType.TexturePicker)]
    public readonly ConfigValue<string> BackgroundTexture = new("");


    // Formatting, scaling

    [ConfigInfo("Automatically scale HUD.")]
    [OptionMenu(OptionSectionType.Hud, "Autoscale HUD", spacer: true)]
    public readonly ConfigValue<bool> AutoScale = new(true);

    [ConfigInfo("Amount to scale the HUD. Autoscale HUD must be disabled to change.")]
    [OptionMenu(OptionSectionType.Hud, "HUD Scale", sliderMin: 0, sliderMax: 5.0, sliderStep: 1)]
    public readonly ConfigValue<double> Scale = new(2.0, Greater(0.0));

    [ConfigInfo("Amount of HUD transparency.")]
    [OptionMenu(OptionSectionType.Hud, "HUD Transparency", sliderMin: 0, sliderMax: 1.0, sliderStep: .05)]
    public readonly ConfigValue<double> Transparency = new(0.0, ClampNormalized);

    [ConfigInfo("Max HUD messages.")]
    [OptionMenu(OptionSectionType.Hud, "Max HUD Messages", sliderMin: 0, sliderMax: 50, sliderStep: 1)]
    public readonly ConfigValue<int> MaxMessages = new(4, GreaterOrEqual(0));

    [ConfigInfo("HUD width as a percentage of the original Doom status bar width; 0 = Max screen width.")]
    [OptionMenu(OptionSectionType.Hud, "Width", sliderMin: 0, sliderMax: 10.0, sliderStep: .05)]
    public readonly ConfigValue<double> Width = new(1, Clamp(0, double.MaxValue));

    [ConfigInfo("Font upscaling ratio (1 - 5); uses xBRZ algorithm to improve text readability", restartRequired: true)]
    [OptionMenu(OptionSectionType.Hud, "Font Upscale Ratio", sliderMin: 1, sliderMax: 5, sliderStep: 1)]
    public readonly ConfigValue<int> FontUpscalingFactor = new(1, Clamp(1, 5));

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
