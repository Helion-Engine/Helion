using Helion.Render.OpenGL.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader.Fields;

public class UniformFloat : UniformElement<float>
{
    public override void Set(IGLFunctions gl, float value)
    {
        Precondition(Location != NoLocation, "Uniform float value did not have the location set");

        gl.Uniform1(Location, value);
    }
}
