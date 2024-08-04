using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Util.Configs.Components;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public readonly struct ShaderUniforms(mat4 mvp, mat4 mvpNoPitch, float timeFrac, bool drawInvulnerability, float mix, int extraLight, float distanceOffset,
    Vec3F colorMix, float fuzzDiv, int colorMapIndex, PaletteIndex paletteIndex, RenderLightMode lightMode)
{
    public readonly mat4 Mvp = mvp;
    public readonly mat4 MvpNoPitch = mvpNoPitch;
    public readonly float TimeFrac = timeFrac;
    public readonly float Mix = mix;
    public readonly bool DrawInvulnerability = drawInvulnerability;
    public readonly int ExtraLight = extraLight;
    public readonly float DistanceOffset = distanceOffset;
    public readonly Vec3F ColorMix = colorMix;
    public readonly float FuzzDiv = fuzzDiv;
    public readonly int ColorMapIndex = colorMapIndex;
    public readonly PaletteIndex PaletteIndex = paletteIndex;
    public readonly RenderLightMode LightMode = lightMode;
}
