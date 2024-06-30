using GlmSharp;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereShader : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_mvpLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_scaleULocation;
    private readonly int m_flipULocation;

    public SkySphereShader() : base("Sky sphere")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_scaleULocation = Uniforms.GetLocation("scaleU");
        m_flipULocation = Uniforms.GetLocation("flipU");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);
    public void ScaleU(float u) => Uniforms.Set(u, m_scaleULocation);
    public void FlipU(bool flip) => Uniforms.Set(flip, m_flipULocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;

        uniform mat4 mvp;
        uniform int flipU;

        void main() {
            uvFrag = uv;
            if (flipU == 0) {
                uvFrag.x = -uvFrag.x;
            }

            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform float scaleU;
        uniform sampler2D boundTexture;
        uniform int hasInvulnerability;

        void main() {
            fragColor = texture(boundTexture, vec2(uvFrag.x * scaleU, uvFrag.y));
            ${InvulnerabilityFragColor}
        }
    "
    .Replace("${InvulnerabilityFragColor}", FragFunction.InvulnerabilityFragColor);
}
