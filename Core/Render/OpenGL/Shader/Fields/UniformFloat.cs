using Helion.Render.OpenGL.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader.Fields
{
    public class UniformFloat : UniformElement<float>
    {
        public UniformFloat(IGLFunctions functions) : base(functions)
        {
        }
        
        public override void Set(float value)
        {
            Precondition(Location != NoLocation, "Uniform float value did not have the location set");
            
            gl.Uniform1(Location, value);
        }
    }
}