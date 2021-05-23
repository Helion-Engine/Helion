using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shaders.Uniforms
{
    public class UniformFloat : UniformElement<float>
    {
        public override void Set(float value)
        {
            Precondition(Location != NoLocation, "Uniform float value did not have the location set");
            
            GL.Uniform1(Location, value);
        }
    }
}