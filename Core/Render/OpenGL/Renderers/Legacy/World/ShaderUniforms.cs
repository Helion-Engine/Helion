using GlmSharp;
using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public readonly struct ShaderUniforms
{
    public readonly mat4 Mvp;
    public readonly mat4 MvpNoPitch;
    public readonly float TimeFrac;
    public readonly float Mix;
    public readonly bool DrawInvulnerability;
    public readonly int ExtraLight;
    public readonly float DistanceOffset;
    public readonly Vec3F ColorMix;

    public ShaderUniforms(mat4 mvp, mat4 mvpNoPitch, float timeFrac, bool drawInvulnerability, float mix, int extraLight, float distanceOffset, Vec3F colorMix)
    {
        Mvp = mvp;
        MvpNoPitch = mvpNoPitch;
        TimeFrac = timeFrac;
        Mix = mix;
        DrawInvulnerability = drawInvulnerability;
        ExtraLight = extraLight;
        DistanceOffset = distanceOffset;
        ColorMix = colorMix;
    }
}
