using GlmSharp;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereGeometryShader : RenderProgram
{
    public SkySphereGeometryShader() : base("Sky sphere geometry")
    {
    }

    public void Mvp(mat4 mat) => Uniforms.Set(mat, "mvp");

    protected override string VertexShader => @"
        #version 330

        layout(location = 0) in vec3 pos;

        uniform mat4 mvp;

        void main() {
            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string? FragmentShader => @"
        #version 130

        out vec4 fragColor;

        void main() {
            fragColor = vec4(1.0, 1.0, 1.0, 1.0);
        }
    ";
}
