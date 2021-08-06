using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shaders.Uniforms
{
    public class UniformInt : UniformElement<int>
    {
        public override void Set(int value)
        {
            Precondition(Location != NoLocation, "Uniform int value did not have the location set");
            
            GL.Uniform1(Location, value);
        }
    }
}