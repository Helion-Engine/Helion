using Helion.Render.Common.Textures;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public enum RenderVsyncMode
{
    Off,
    On,
    Adaptive
}

public class ConfigRenderFilter
{
    [ConfigInfo("The kind of filter applied to fonts.")]
    [OptionMenu(OptionSectionType.Render, "Font filtering")]
    public readonly ConfigValue<FilterType> Font = new(FilterType.Nearest, OnlyValidEnums<FilterType>());

    [ConfigInfo("The filter to be applied to textures.")]
    [OptionMenu(OptionSectionType.Render, "Texture filtering")]
    public readonly ConfigValue<FilterType> Texture = new(FilterType.Nearest, OnlyValidEnums<FilterType>());
}

public class ConfigRender
{
    [ConfigInfo("If VSync should be on or off. Prevents tearing, but affects input processing (unless you have g-sync).")]
    [OptionMenu(OptionSectionType.Render, "VSync")]
    public readonly ConfigValue<RenderVsyncMode> VSync = new(RenderVsyncMode.On);

    [ConfigInfo("A cap on the maximum amount of frames per second. Zero is equivalent to no cap.")]
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

    public readonly ConfigRenderFilter Filter = new();

    [ConfigInfo("Field of view. Default = 90")]
    [OptionMenu(OptionSectionType.Render, "Field of view")]
    public readonly ConfigValue<double> FieldOfView = new(90, Clamp(60.0, 120.0));

    [ConfigInfo("Max render distance.")]
    [OptionMenu(OptionSectionType.Render, "Max rendering distance")]
    public readonly ConfigValue<int> MaxDistance = new(0);

    [ConfigInfo("Enable sprite transparency.")]
    [OptionMenu(OptionSectionType.Render, "Sprite transparency", spacer: true)]
    public readonly ConfigValue<bool> SpriteTransparency = new(true);

    [ConfigInfo("Enable texture transparency.")]
    [OptionMenu(OptionSectionType.Render, "Texture transparency")]
    public readonly ConfigValue<bool> TextureTransparency = new(true);

    [ConfigInfo("The anisotropic filtering amount. A value of 1 is the same as being off.")]
    [OptionMenu(OptionSectionType.Render, "Anisotropy")]
    public readonly ConfigValue<int> Anisotropy = new(8, GreaterOrEqual(1));

    [ConfigInfo("Emulate fake contrast like vanilla Doom.")]
    [OptionMenu(OptionSectionType.Render, "Emulate vanilla contrast")]
    public readonly ConfigValue<bool> FakeContrast = new(true);

    [ConfigInfo("If true, forces the pipeline to be flushed after rendering a frame. May fix a laggy buffered feeling on lower end computers.")]
    [OptionMenu(OptionSectionType.Render, "Pipeline flush (for old GPUs)")]
    public readonly ConfigValue<bool> ForcePipelineFlush = new(false);

    [ConfigInfo("The multisampling amount. A value of 1 is the same as being off.")]
    public readonly ConfigValue<int> Multisample = new(1, GreaterOrEqual(1));

    [ConfigInfo("If any sprite should clip the floor.")]
    [OptionMenu(OptionSectionType.Render, "Sprite floor clip", spacer: true)]
    public readonly ConfigValue<bool> SpriteClip = new(true);

    [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
    [OptionMenu(OptionSectionType.Render, "Clip max height percentage")]
    public readonly ConfigValue<double> SpriteClipFactorMax = new(0.02, ClampNormalized);

    [ConfigInfo("The minimum sprite height to allow to clip the floor.")]
    [OptionMenu(OptionSectionType.Render, "Clip min height")]
    public readonly ConfigValue<int> SpriteClipMin = new(16, GreaterOrEqual(0));

    [ConfigInfo("Checks if sprites will overlap and z-fight.")]
    [OptionMenu(OptionSectionType.Render, "Sprite Z-fighting check")]
    public readonly ConfigValue<bool> SpriteZCheck = new(true);

    [ConfigInfo("Adds to the rendering light level offset.")]
    [OptionMenu(OptionSectionType.Render, "Extra lighting", spacer: true)]
    public readonly ConfigValue<int> ExtraLight = new(0);

    [ConfigInfo("Draws everything at full brightness.")]
    [OptionMenu(OptionSectionType.Render, "Full brightness")]
    public readonly ConfigValue<bool> Fullbright = new(false);

    [ConfigInfo("Traverses the BSP in a separate thread to mark lines seen for automap. Ignored if using BSP rendering.")]
    [OptionMenu(OptionSectionType.Render, "Automap on separate thread")]
    public readonly ConfigValue<bool> AutomapBspThread = new(true);

    [ConfigInfo("Enable rendering missing textures as a red/black checkered texture.", mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.Render, "Render null textures")]
    public readonly ConfigValue<bool> NullTexture = new(false);
}
