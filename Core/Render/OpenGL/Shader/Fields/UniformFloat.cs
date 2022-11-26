using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader.Fields;

public class UniformFloat : UniformElement<float>
{
    public override void Set(float value)
    {
        Precondition(Location != NoLocation, "Uniform float value did not have the location set");

        GL.Uniform1(Location, value);
    }
}
