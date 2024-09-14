using GlmSharp;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereGeometryShader : RenderProgram
{
    private readonly int m_mvpLocation;
    private readonly int m_timeFracLocation;

    public SkySphereGeometryShader() : base("Sky sphere geometry")
    {
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_timeFracLocation = Uniforms.GetLocation("timeFrac");
    }

    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);
    public void TimeFrac(float value) => Uniforms.Set(value, m_timeFracLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float prevZ;

        uniform mat4 mvp;
        uniform float timeFrac;

        void main() {
            float z = mix(prevZ, pos.z, timeFrac);
            gl_Position = mvp * vec4(pos.x, pos.y, z, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        out vec4 fragColor;

        void main() {
            fragColor = vec4(1.0, 1.0, 1.0, 1.0);
        }
    ";
}
