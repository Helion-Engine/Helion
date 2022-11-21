using GlmSharp;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader.Fields;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shader.Fields;

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
