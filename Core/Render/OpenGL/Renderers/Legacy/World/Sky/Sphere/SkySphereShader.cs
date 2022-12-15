using GlmSharp;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereShader : RenderProgram
{
    public SkySphereShader() : base("Sky sphere")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, "boundTexture");
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, "hasInvulnerability");
    public void Mvp(mat4 mat) => Uniforms.Set(mat, "mvp");
    public void ScaleU(float u) => Uniforms.Set(u, "scaleU");
    public void FlipU(bool flip) => Uniforms.Set(flip, "flipU");

    protected override string VertexShader => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;

        uniform mat4 mvp;
        uniform int flipU;

        void main() {
            uvFrag = uv;
            if (flipU != 0) {
                uvFrag.x = -uvFrag.x;
            }

            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string? FragmentShader => @"
        #version 130

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform float scaleU;
        uniform sampler2D boundTexture;
        uniform int hasInvulnerability;

        void main() {
            fragColor = texture(boundTexture, vec2(uvFrag.x * scaleU, uvFrag.y));

            // If invulnerable, grayscale everything and crank the brightness.
            // Note: The 1.5x is a visual guess to make it look closer to vanilla.
            if (hasInvulnerability != 0)
            {
                float maxColor = max(max(fragColor.x, fragColor.y), fragColor.z);
                maxColor *= 1.5;
                fragColor.xyz = vec3(maxColor, maxColor, maxColor);
            }
        }
    ";
}
