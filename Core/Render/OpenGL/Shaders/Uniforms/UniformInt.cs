using OpenTK.Graphics.OpenGL4;
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
        
        /// <summary>
        /// Since we use an integer value as a sampler, this is a helper function
        /// to help make it easier on us.
        /// </summary>
        /// <param name="textureUnit">The unit to set into the uniform.</param>
        public void Set(TextureUnit textureUnit)
        {
            Precondition(Location != NoLocation, "Uniform (sampler) int value did not have the location set");
            
            GL.Uniform1(Location, (int)textureUnit - (int)TextureUnit.Texture0);
        }
    }
}