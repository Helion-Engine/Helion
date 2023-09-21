using GlmSharp;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill.Optimized;

public class FloodFillProgram : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_mvpLocation;

    public FloodFillProgram() : base("Flood fill plane")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    
    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;

        uniform mat4 mvp;

        void main()
        {
            gl_Position = mvp * vec4(pos.xy, z, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        out vec4 fragColor;

        uniform sampler2D boundTexture;

        void main()
        {
            fragColor= vec4(1.0, 1.0, 1.0, 1.0);
        }
    ";
}
