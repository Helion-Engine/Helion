using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Util.Configs.Components;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public record struct ColorMapUniforms(int GlobalIndex, int SkyIndex, int SectorIndex);
public record struct ColorMixUniforms(Vec3F Global, Vec3F Sky, Vec3F Sector);

public readonly struct ShaderUniforms(mat4 mvp, mat4 mvpNoPitch, float timeFrac, bool drawInvulnerability, float mix, int extraLight, float distanceOffset,
    ColorMixUniforms colorMix, float fuzzDiv, ColorMapUniforms colorMapUniforms, PaletteIndex paletteIndex, RenderLightMode lightMode)
{
    public readonly mat4 Mvp = mvp;
    public readonly mat4 MvpNoPitch = mvpNoPitch;
    public readonly float TimeFrac = timeFrac;
    public readonly float Mix = mix;
    public readonly bool DrawInvulnerability = drawInvulnerability;
    public readonly int ExtraLight = extraLight;
    public readonly float DistanceOffset = distanceOffset;
    public readonly ColorMixUniforms ColorMix = colorMix;
    public readonly float FuzzDiv = fuzzDiv;
    public readonly ColorMapUniforms ColorMapUniforms = colorMapUniforms;
    public readonly PaletteIndex PaletteIndex = paletteIndex;
    public readonly RenderLightMode LightMode = lightMode;
}
