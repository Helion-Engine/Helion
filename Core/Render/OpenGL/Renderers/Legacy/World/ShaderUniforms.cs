using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Util.Configs.Components;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public record struct ColorMapUniforms(int GlobalIndex, int SkyIndex, int SectorIndex);
public record struct ColorMixUniforms(Vec3F Global, Vec3F Sky, Vec3F Sector);

public struct ShaderUniforms(mat4 mvp, mat4 mvpNoPitch, float timeFrac, bool drawInvulnerability, float mix, int extraLight, float distanceOffset,
    ColorMixUniforms colorMix, float fuzzDiv, ColorMapUniforms colorMapUniforms, PaletteIndex paletteIndex, RenderLightMode lightMode, float gammaCorrection)
{
    public mat4 Mvp = mvp;
    public mat4 MvpNoPitch = mvpNoPitch;
    public float TimeFrac = timeFrac;
    public float Mix = mix;
    public bool DrawInvulnerability = drawInvulnerability;
    public int ExtraLight = extraLight;
    public float DistanceOffset = distanceOffset;
    public ColorMixUniforms ColorMix = colorMix;
    public float FuzzDiv = fuzzDiv;
    public ColorMapUniforms ColorMapUniforms = colorMapUniforms;
    public PaletteIndex PaletteIndex = paletteIndex;
    public RenderLightMode LightMode = lightMode;
    public float GammaCorrection = gammaCorrection;
}
