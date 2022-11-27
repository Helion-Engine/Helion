using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap;

public class AutomapShader : RenderProgram
{
    public AutomapShader() : base("Automap")
    {
    }

    public void Color(Vec3F color) => Uniforms["color"] = color;
    public void Mvp(mat4 mat) => Uniforms["mvp"] = mat;

    protected override string VertexShader() => @"
        #version 130

        in vec2 pos;

        uniform mat4 mvp;

        void main() {
            gl_Position = mvp * vec4(pos, 0.5, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 130

        out vec4 fragColor;

        uniform vec3 color;

        void main() {
            fragColor = vec4(color, 1.0f);
        }
    ";
}
