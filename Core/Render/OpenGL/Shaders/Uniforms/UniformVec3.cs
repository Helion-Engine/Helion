using GlmSharp;
using Helion.Geometry.Vectors;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shaders.Uniforms
{
    public class UniformVec3 : UniformElement<vec3>
    {
        public override void Set(vec3 value)
        {
            Set(value.x, value.y, value.z);
        }
        
        public void Set(Vec3F value)
        {
            Set(value.X, value.Y, value.Z);
        }
        
        public void Set(float x, float y, float z)
        {
            GL.Uniform3(Location, x, y, z);
        }
    }
}
