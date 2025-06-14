using Helion.Render.Common.Textures;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using System.ComponentModel;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public enum RenderVsyncMode
{
    Off,
    On,
    Adaptive
}

public enum RenderColorMode
{
    [Description("True Color")]
    TrueColor,
    Palette
}

public enum RenderLightMode
{
    Banded,
    Smooth
}

public enum SkyRenderMode
{
    Vanilla,
    Dynamic
}

public enum RenderContrastMode
{
    Off,
    Vanilla,
    Smooth
}

public class ConfigRenderFilter : ConfigElement<ConfigRenderFilter>
{
    [ConfigInfo("Filter applied to fonts.")]
    // TODO need to implement to take effect
    //[OptionMenu(OptionSectionType.Render, "Font filtering")]
    public readonly ConfigValue<FilterType> Font = new(FilterType.Nearest, OnlyValidEnums<FilterType>());

    [ConfigInfo("Filter applied to textures. True color required.")]
    [OptionMenu(OptionSectionType.Render, "Texture Filtering")]
    public readonly ConfigValue<FilterType> Texture = new(FilterType.Nearest, OnlyValidEnums<FilterType>());
}

public class ConfigRenderHealthBar : ConfigElement<ConfigRenderHealthBar>
{
    [ConfigInfo("Renders health bars above shootable things.")]
    [OptionMenu(OptionSectionType.Render, "Enable")]
    public readonly ConfigValue<bool> Enable = new(false);

    [ConfigInfo("Flashes health bar while enemy is attacking.")]
    [OptionMenu(OptionSectionType.Render, "Attack Indicator")]
    public readonly ConfigValue<bool> AttackIndicator = new(false);

    [ConfigInfo("Shows health bar when max health is equal to or over this limit.")]
    [OptionMenu(OptionSectionType.Render, "Health Limit")]
    public readonly ConfigValue<int> HealthLimit = new(0, GreaterOrEqual(0));
}

public class ConfigRender: ConfigElement<ConfigRender>
{
    // VSync and rate limiting

    [ConfigInfo("Vertical synchronization. Prevents tearing, but affects input processing (unless you have G-Sync).")]
    [OptionMenu(OptionSectionType.Render, "VSync")]
    public readonly ConfigValue<RenderVsyncMode> VSync = new(RenderVsyncMode.On);

    [ConfigInfo("Maximum frames per second. Zero is equivalent to no cap if vsync is off (or monitor refresh rate if vsync is on/adaptive).")]
    [OptionMenu(OptionSectionType.Render, "Max FPS", sliderMin: 0, sliderMax: 250, sliderStep: 1)]
    public readonly ConfigValue<int> MaxFPS = new(0, fps =>
    {
        return fps switch
        {
            <= 0 => 0,
            < 35 => 35,
            _ => fps
        };
    });


    // Textures and filtering

    [ConfigInfo("Anisotropic filtering amount. A value of 1 is the same as being off. True color required.")]
    [OptionMenu(OptionSectionType.Render, "Anisotropy", spacer: true, sliderMin: 0, sliderMax: 16, sliderStep: 1)]
    public readonly ConfigValue<int> Anisotropy = new(8, GreaterOrEqual(1));

    public readonly ConfigRenderFilter Filter = new();

    [ConfigInfo("Render missing textures as a red/black checkered texture.", mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Render Null Textures", spacer: true)]
    public readonly ConfigValue<bool> NullTexture = new(false);


    // Viewport

    [ConfigInfo("Field of view.")]
    [OptionMenu(OptionSectionType.Render, "Field Of View", spacer:true, sliderMin: 60.0, sliderMax: 120.0, sliderStep: .5)]
    public readonly ConfigValue<double> FieldOfView = new(90, Clamp(60.0, 120.0));

    [ConfigInfo("Max render distance.")]
    [OptionMenu(OptionSectionType.Render, "Max Rendering Distance", sliderMin: 0.0, sliderMax: int.MaxValue, sliderStep: 100)]
    public readonly ConfigValue<int> MaxDistance = new(0);


    // Lighting effects

    [ConfigInfo("Set light projection to banded or smooth. Smooth only supported with true color rendering.")]
    [OptionMenu(OptionSectionType.Render, "Light Mode", spacer: true)]
    public readonly ConfigValue<RenderLightMode> LightMode = new(RenderLightMode.Smooth);

    [ConfigInfo("Added light level offset.")]
    [OptionMenu(OptionSectionType.Render, "Extra Lighting", sliderMin: 0, sliderMax: 10.0, sliderStep: .1)]
    public readonly ConfigValue<int> ExtraLight = new(0);

    [ConfigInfo("Draw everything at full brightness.")]
    [OptionMenu(OptionSectionType.Render, "Full Brightness")]
    public readonly ConfigValue<bool> Fullbright = new(false);

    [ConfigInfo("Use ZDoom-compatible brightmaps, if loaded. True color required.")]
    [OptionMenu(OptionSectionType.Render, "Use Brightmaps")]
    public readonly ConfigValue<bool> Brightmaps = new(true);


    // Misc. Visual effects

    [ConfigInfo("Gamma correction level.")]
    [OptionMenu(OptionSectionType.Render, "Gamma correction", spacer: true, sliderMin: 1.0, sliderMax: 4.0, sliderStep: .1)]
    public readonly ConfigValue<double> GammaCorrection = new(1, Clamp(1.0, 4.0));

    [ConfigInfo("Emulate fake contrast like vanilla Doom.", legacy: true)]
    public readonly ConfigValue<bool> FakeContrast = new(true);

    [ConfigInfo("Fuzz amount for partial invisibility effect.")]
    [OptionMenu(OptionSectionType.Render, "Fuzz Amount", sliderMin: 0, sliderMax: 5.0, sliderStep: .1)]
    public readonly ConfigValue<double> FuzzAmount = new(1);

    [ConfigInfo("Prevent sprites from overlapping and Z-fighting.")]
    [OptionMenu(OptionSectionType.Render, "Sprite Z-fighting Check")]
    public readonly ConfigValue<bool> SpriteZCheck = new(true);

    [ConfigInfo("Enable sprite transparency.")]
    [OptionMenu(OptionSectionType.Render, "Sprite Transparency")]
    public readonly ConfigValue<bool> SpriteTransparency = new(true);

    [ConfigInfo("Render sprites over floors/ceilings. Sprites always clipped to walls. May slow down rendering.", mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Emulate Vanilla Rendering", spacer: true)]
    public readonly ConfigValue<bool> VanillaRender = new(false);

    [ConfigInfo("Emulates custom invulnerability palettes in true color mode. May not work well with all WADs. Application restart required.", restartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Emulate Invulnerability Colormap")]
    public readonly ConfigValue<bool> EmulateInvulnerabilityColorMap = new(false);

    [ConfigInfo("Line contrast mode.", mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Line contrast mode")]
    public readonly ConfigValue<RenderContrastMode> ContrastMode = new(RenderContrastMode.Vanilla);

    [ConfigInfo("Sky render mode")]
    [OptionMenu(OptionSectionType.Render, "Sky Render Mode")]
    public readonly ConfigValue<SkyRenderMode> SkyMode = new(SkyRenderMode.Dynamic);

    [ConfigInfo("Pushes line vertices a tiny amount to cover potential pixel gaps from rendering precision errors.", mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Pixel Gap Correction", spacer: true)]
    public readonly ConfigValue<bool> PixelGapCorrection = new(true);

    [ConfigInfo("", save: false, legacy: true)]
    [OptionMenu(OptionSectionType.Render, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader HealthBarHeader = new("Health Bar");
    public readonly ConfigRenderHealthBar HealthBar = new();

    // Settings below are believed to be less frequently used and thus are not on the menus.

    [ConfigInfo("Cache all sprites. Prevents stuttering compared to loading them at runtime.", restartRequired: true)]
    public readonly ConfigValue<bool> CacheSprites = new(true);

    [ConfigInfo("Force pipeline flush after rendering each frame. May fix a laggy buffered feeling on lower end computers.")]
    public readonly ConfigValue<bool> ForcePipelineFlush = new(false);

    [ConfigInfo("Clip sprites against the floor.")]
    public readonly ConfigValue<bool> SpriteClip = new(true);

    [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
    public readonly ConfigValue<double> SpriteClipFactorMax = new(0.02, ClampNormalized);

    [ConfigInfo("Minimum sprite height to allow to clip the floor.")]
    public readonly ConfigValue<int> SpriteClipMin = new(16, GreaterOrEqual(0));

    [ConfigInfo("Enable texture transparency.")]
    public readonly ConfigValue<bool> TextureTransparency = new(true);

    [ConfigInfo("Traverse the BSP tree in a separate thread to mark lines seen for automap. If disabled, automap always shows all lines.")]
    public readonly ConfigValue<bool> AutomapBspThread = new(true);
}
