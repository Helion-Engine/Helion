using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap;

public class AutomapShader : RenderProgram
{
    private readonly int m_colorLocation;
    private readonly int m_mvpLocation;

    public AutomapShader() : base("Automap")
    {
        m_colorLocation = Uniforms.GetLocation("color");
        m_mvpLocation = Uniforms.GetLocation("mvp");
    }

    public void Color(Vec3F color) => Uniforms.Set(color, m_colorLocation);
    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec2 pos;

        uniform mat4 mvp;

        void main() {
            gl_Position = mvp * vec4(pos, 0.5, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        out vec4 fragColor;

        uniform vec3 color;

        void main() {
            fragColor = vec4(color, 1.0f);
        }
    ";
}
