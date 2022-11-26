using GlmSharp;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereGeometryShader : RenderShader
{
    public SkySphereGeometryShader() : base("Program: Sky sphere geometry")
    {
    }

    public void Mvp(mat4 mat) => Uniforms["mvp"] = mat;

    protected override string VertexShader() => @"
        #version 130

        in vec3 pos;

        uniform mat4 mvp;

        void main() {
            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 130

        out vec4 fragColor;

        void main() {
            fragColor = vec4(1.0, 1.0, 1.0, 1.0);
        }
    ";
}
