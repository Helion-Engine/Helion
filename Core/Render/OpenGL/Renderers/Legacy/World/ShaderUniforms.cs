using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Util.Configs.Components;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public record struct ColorMapUniforms(int GlobalIndex, int SkyIndex, int SectorIndex);
public record struct ColorMixUniforms(Vec3F Global, Vec3F Sky, Vec3F Sector);

public struct ShaderUniforms(
    mat4 mvp,
    mat4 mvpNoPitch,
    float timeFrac,
    bool drawInvulnerability,
    float mix,
    int extraLightOrColorMapIndex,
    float distanceOffset,
    ColorMixUniforms colorMix,
    float fuzzDiv,
    ColorMapUniforms colorMapUniforms,
    PaletteIndex paletteIndex,
    RenderLightMode lightMode,
    float gammaCorrection,
    int maxDistance,
    bool useBrightmaps,
    bool sectorColor,
    bool sectorFog,
    int viewBlendSectorFogIndex,
    float downScaleAmount)
{
    public const int ViewBlendFog = 2;
    public const int NoViewBlendSectorIndex = -1;

    public mat4 Mvp = mvp;
    public mat4 MvpNoPitch = mvpNoPitch;
    public float TimeFrac = timeFrac;
    public float Mix = mix;
    public bool DrawInvulnerability = drawInvulnerability;
    public int ExtraLightOrColorMapIndex = extraLightOrColorMapIndex;
    public float DistanceOffset = distanceOffset;
    public ColorMixUniforms ColorMix = colorMix;
    public float FuzzDiv = fuzzDiv;
    public ColorMapUniforms ColorMapUniforms = colorMapUniforms;
    public PaletteIndex PaletteIndex = paletteIndex;
    public RenderLightMode LightMode = lightMode;
    public float GammaCorrection = gammaCorrection;
    public int MaxDistance = maxDistance;
    public bool UseBrightmaps = useBrightmaps;
    public bool SectorColor = sectorColor;
    public bool SectorFog = sectorFog;
     public int SectorFogIndex = sectorFog ? viewBlendSectorFogIndex >= 0 ? ViewBlendFog : 1 : 0;
    public int ViewBlendFogSectorIndex = viewBlendSectorFogIndex;
    public float DownScaleAmount = downScaleAmount;
}
