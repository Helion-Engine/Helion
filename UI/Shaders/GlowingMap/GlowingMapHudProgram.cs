using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.UI.Shaders.GlowingMap;

public class GlowingMapHudProgram : RenderProgram
{
    public void ColorScale(Vec3F colorScale) => Uniforms.Set(colorScale, "colorScale");
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, "mvp");
    public void Tex(TextureUnit unit) => Uniforms.Set(unit, "tex");
    
    public GlowingMapHudProgram() : base("Glowing map HUD")
    {
    }

    protected override string VertexShader()
    {
        return @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;

        uniform mat4 mvp;

        void main()
        {
            uvFrag = uv;

            gl_Position = mvp * vec4(pos, 1);
        }
        ";
    }

    protected override string FragmentShader()
    {
        return @"
        #version 330

        in vec2 uvFrag;
        
        out vec4 fragColor;

        uniform vec3 colorScale;
        uniform sampler2D tex;

        void main()
        {
            fragColor = texture(tex, uvFrag);
            fragColor.xyz *= colorScale;
        }
        ";
    }
}