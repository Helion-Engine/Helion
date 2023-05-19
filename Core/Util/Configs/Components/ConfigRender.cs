using Helion.Geometry;
using Helion.Render.Common.Textures;
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
    public readonly ConfigValue<FilterType> Font = new(FilterType.Nearest, OnlyValidEnums<FilterType>());

    [ConfigInfo("The filter to be applied to textures.")]
    public readonly ConfigValue<FilterType> Texture = new(FilterType.Nearest, OnlyValidEnums<FilterType>());
}

public class ConfigRender
{
    [ConfigInfo("The anisotropic filtering amount. A value of 1 is the same as being off.")]
    public readonly ConfigValue<int> Anisotropy = new(8, GreaterOrEqual(1));

    [ConfigInfo("Emulate fake contrast like vanilla Doom.")]
    public readonly ConfigValue<bool> FakeContrast = new(true);

    public readonly ConfigRenderFilter Filter = new();

    [ConfigInfo("If true, forces the pipeline to be flushed after rendering a frame. May fix a laggy buffered feeling on lower end computers.")]
    public readonly ConfigValue<bool> ForcePipelineFlush = new(false);

    [ConfigInfo("A cap on the maximum amount of frames per second. Zero is equivalent to no cap.")]
    public readonly ConfigValue<int> MaxFPS = new(250, fps =>
    {
        return fps switch
        {
            <= 0 => 0,
            < 35 => 35,
            _ => fps
        };
    });

    [ConfigInfo("The multisampling amount. A value of 1 is the same as being off.")]
    public readonly ConfigValue<int> Multisample = new(1, GreaterOrEqual(1));

    [ConfigInfo("If any sprite should clip the floor.")]
    public readonly ConfigValue<bool> SpriteClip = new(true);

    [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
    public readonly ConfigValue<double> SpriteClipFactorMax = new(0.02, ClampNormalized);

    [ConfigInfo("The minimum sprite height to allow to clip the floor.")]
    public readonly ConfigValue<int> SpriteClipMin = new(16, GreaterOrEqual(0));

    [ConfigInfo("Checks if sprites will overlap and z-fight.")]
    public readonly ConfigValue<bool> SpriteZCheck = new(true);

    [ConfigInfo("If VSync should be on or off. Prevents tearing, but affects input processing (unless you have g-sync).")]
    public readonly ConfigValue<RenderVsyncMode> VSync = new(RenderVsyncMode.On);

    [ConfigInfo("Adds to the rendering light level offset.")]
    public readonly ConfigValue<int> ExtraLight = new(0);

    [ConfigInfo("Draws everything at full brightness.")]
    public readonly ConfigValue<bool> Fullbright = new(false);

    [ConfigInfo("Enable sprite transparency.")]
    public readonly ConfigValue<bool> SpriteTransparency = new(true);

    [ConfigInfo("Enable texture transparency.")]
    public readonly ConfigValue<bool> TextureTransparency = new(true);

    [ConfigInfo("Max render distance.")]
    public readonly ConfigValue<int> MaxDistance = new(0);

    [ConfigInfo("Static rendering mode.", restartRequired: true)]
    public readonly ConfigValue<bool> StaticMode = new(true);

    [ConfigInfo("Update scrolling walls for static rendering.", restartRequired: true)]
    public readonly ConfigValue<bool> StaticScroll = new(false);

    [ConfigInfo("Use blockmap rendering. Static mode required.")]
    public readonly ConfigValue<bool> Blockmap = new(true);

    [ConfigInfo("Traverses the BSP in a separate thread to mark lines seen for automap. Ignored if using BSP rendering.")]
    public readonly ConfigValue<bool> AutomapBspThread = new(true);

    [ConfigInfo("Field of view. Default = 90")]
    public readonly ConfigValue<double> FieldOfView = new(90, Clamp(60, 120));

    [ConfigInfo("Enable sector flood fill.", restartRequired: true)]
    public readonly ConfigValue<bool> FloodFill = new(true);

    [ConfigInfo("Enable alternate method for sector flood fill.", restartRequired: true)]
    public readonly ConfigValue<bool> FloodFillAlt = new(false);

    [ConfigInfo("Enable sprite rendering with single vertex using the geometry shader.")]
    public readonly ConfigValue<bool> SingleVertexSprites = new(false);
}
