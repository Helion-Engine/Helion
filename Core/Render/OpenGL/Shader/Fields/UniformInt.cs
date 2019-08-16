using Helion.Render.OpenGL.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader.Fields
{
    public class UniformInt : UniformElement<int>
    {
        public override void Set(IGLFunctions gl, int value)
        {
            Precondition(Location != NoLocation, "Uniform int value did not have the location set");
            
            gl.Uniform1(Location, value);
        }
    }
}