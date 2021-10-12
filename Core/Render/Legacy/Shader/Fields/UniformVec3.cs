using GlmSharp;
using Helion.Render.Legacy.Context;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.Legacy.Shader.Fields;

public class UniformVec3 : UniformElement<vec3>
{
    public override void Set(IGLFunctions gl, vec3 value)
    {
        Set(gl, value.x, value.y, value.z);
    }

    public void Set(IGLFunctions gl, float x, float y, float z)
    {
        GL.Uniform3(Location, x, y, z);
    }
}

