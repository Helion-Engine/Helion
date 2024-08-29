using Helion.Render.Common.Textures;
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

public class ConfigRenderFilter
{
    [ConfigInfo("Filter applied to fonts.")]
    // TODO need to implement to take effect
    //[OptionMenu(OptionSectionType.Render, "Font filtering")]
    public readonly ConfigValue<FilterType> Font = new(FilterType.Nearest, OnlyValidEnums<FilterType>());

    [ConfigInfo("Filter applied to textures. True color required.")]
    [OptionMenu(OptionSectionType.Render, "Texture Filtering", spacer: true)]
    public readonly ConfigValue<FilterType> Texture = new(FilterType.Nearest, OnlyValidEnums<FilterType>());
}

public class ConfigRender
{
    [ConfigInfo("Vertical synchronization. Prevents tearing, but affects input processing (unless you have G-Sync).")]
    [OptionMenu(OptionSectionType.Render, "VSync")]
    public readonly ConfigValue<RenderVsyncMode> VSync = new(RenderVsyncMode.On);

    [ConfigInfo("Maximum frames per second. Zero is equivalent to no cap.")]
    [OptionMenu(OptionSectionType.Render, "Max FPS")]
    public readonly ConfigValue<int> MaxFPS = new(250, fps =>
    {
        return fps switch
        {
            <= 0 => 0,
            < 35 => 35,
            _ => fps
        };
    });

    [ConfigInfo("Field of view.")]
    [OptionMenu(OptionSectionType.Render, "Field Of View")]
    public readonly ConfigValue<double> FieldOfView = new(90, Clamp(60.0, 120.0));

    [ConfigInfo("Max render distance.")]
    [OptionMenu(OptionSectionType.Render, "Max Rendering Distance")]
    public readonly ConfigValue<int> MaxDistance = new(0);

    public readonly ConfigRenderFilter Filter = new();

    [ConfigInfo("Anisotropic filtering amount. A value of 1 is the same as being off. True color required.")]
    [OptionMenu(OptionSectionType.Render, "Anisotropy")]
    public readonly ConfigValue<int> Anisotropy = new(8, GreaterOrEqual(1));

    [ConfigInfo("Enable sprite transparency.")]
    [OptionMenu(OptionSectionType.Render, "Sprite Transparency", spacer: true)]
    public readonly ConfigValue<bool> SpriteTransparency = new(true);

    [ConfigInfo("Enable texture transparency.")]
    [OptionMenu(OptionSectionType.Render, "Texture Transparency")]
    public readonly ConfigValue<bool> TextureTransparency = new(true);

    [ConfigInfo("Emulate fake contrast like vanilla Doom.")]
    [OptionMenu(OptionSectionType.Render, "Emulate Vanilla Contrast")]
    public readonly ConfigValue<bool> FakeContrast = new(true);

    [ConfigInfo("Render sprites over floors/ceilings. Sprites always clipped to walls.", mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Emulate Vanilla Rendering", spacer: true)]
    public readonly ConfigValue<bool> VanillaRender = new(false);

    [ConfigInfo("Set light projection to banded or smooth. Smooth only supported with true color rendering.")]
    [OptionMenu(OptionSectionType.Render, "Light Mode", spacer: true)]
    public readonly ConfigValue<RenderLightMode> LightMode = new(RenderLightMode.Smooth);

    [ConfigInfo("Added light level offset.")]
    [OptionMenu(OptionSectionType.Render, "Extra Lighting")]
    public readonly ConfigValue<int> ExtraLight = new(0);

    [ConfigInfo("Draw everything at full brightness.")]
    [OptionMenu(OptionSectionType.Render, "Full Brightness")]
    public readonly ConfigValue<bool> Fullbright = new(false);

    [ConfigInfo("Traverse the BSP tree in a separate thread to mark lines seen for automap. If disabled, automap always shows all lines.")]
    [OptionMenu(OptionSectionType.Render, "Automap on Separate Thread")]
    public readonly ConfigValue<bool> AutomapBspThread = new(true);

    [ConfigInfo("Render missing textures as a red/black checkered texture.", mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Render Null Textures")]
    public readonly ConfigValue<bool> NullTexture = new(false);

    [ConfigInfo("Fuzz amount for partial invisibility effect.")]
    [OptionMenu(OptionSectionType.Render, "Fuzz Amount")]
    public readonly ConfigValue<double> FuzzAmount = new(1);

    [ConfigInfo("Prevent sprites from overlapping and Z-fighting.")]
    [OptionMenu(OptionSectionType.Render, "Sprite Z-fighting Check")]
    public readonly ConfigValue<bool> SpriteZCheck = new(true);

    // Settings below are believed to be less frequently used and thus are not on the menus.

    [ConfigInfo("Cache all sprites. Prevents stuttering compared to loading them at runtime.", restartRequired: true)]
    public readonly ConfigValue<bool> CacheSprites = new(true);

    [ConfigInfo("Force pipeline flush after rendering each frame. May fix a laggy buffered feeling on lower end computers.")]
    public readonly ConfigValue<bool> ForcePipelineFlush = new(false);

    [ConfigInfo("Multisampling amount. A value of 1 is the same as being off.")]
    public readonly ConfigValue<int> Multisample = new(1, GreaterOrEqual(1));

    [ConfigInfo("Clip sprites against the floor.")]
    public readonly ConfigValue<bool> SpriteClip = new(true);

    [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
    public readonly ConfigValue<double> SpriteClipFactorMax = new(0.02, ClampNormalized);

    [ConfigInfo("Minimum sprite height to allow to clip the floor.")]
    public readonly ConfigValue<int> SpriteClipMin = new(16, GreaterOrEqual(0));
}
