using GlmSharp;
using Helion.Geometry.Vectors;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL.Shaders.Uniforms;

public class UniformVec2 : UniformElement<vec2>
{
    public override void Set(vec2 value)
    {
        Set(value.x, value.y);
    }

    public void Set(Vec2F value)
    {
        Set(value.X, value.Y);
    }

    public void Set(float x, float y)
    {
        GL.Uniform2(Location, x, y);
    }
}
