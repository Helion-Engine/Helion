using GlmSharp;
using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shader.Fields;

public class UniformVec3 : UniformElement<vec3>
{
    public override void Set(vec3 value)
    {
        Set(value.x, value.y, value.z);
    }

    public void Set(float x, float y, float z)
    {
        GL.Uniform3(Location, x, y, z);
    }
}
