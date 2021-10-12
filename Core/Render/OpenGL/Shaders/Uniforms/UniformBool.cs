using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shaders.Uniforms;

public class UniformBool : UniformElement<bool>
{
    public const int UniformBoolFalse = 0;
    public const int UniformBoolTrue = 1;

    public override void Set(bool value)
    {
        Precondition(Location != NoLocation, "Uniform bool value did not have the location set");

        GL.Uniform1(Location, value ? UniformBoolTrue : UniformBoolFalse);
    }
}

