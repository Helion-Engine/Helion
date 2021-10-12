using GlmSharp;
using Helion.Geometry.Vectors;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shaders.Uniforms;

public class UniformVec4 : UniformElement<vec4>
{
    public override void Set(vec4 value)
    {
        Set(value.x, value.y, value.z, value.w);
    }

    public void Set(Vec4F value)
    {
        Set(value.X, value.Y, value.Z, value.W);
    }

    public void Set(float x, float y, float z, float w)
    {
        GL.Uniform4(Location, x, y, z, w);
    }
}

