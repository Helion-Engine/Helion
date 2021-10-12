using GlmSharp;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shaders.Uniforms;

public class UniformMatrix4 : UniformElement<mat4>
{
    public override void Set(mat4 value)
    {
        Precondition(Location != NoLocation, "Uniform float value did not have the location set");

        GL.UniformMatrix4(Location, 1, false, value.Values1D);
    }
}

