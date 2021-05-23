using Helion.Render.OpenGL.Legacy.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Legacy.Shader.Fields
{
    public class UniformBool : UniformElement<bool>
    {
        public const int UniformBoolFalse = 0;
        public const int UniformBoolTrue = 1;

        public override void Set(IGLFunctions gl, bool value)
        {
            Precondition(Location != NoLocation, "Uniform bool value did not have the location set");

            gl.Uniform1(Location, value ? UniformBoolTrue : UniformBoolFalse);
        }
    }
}
