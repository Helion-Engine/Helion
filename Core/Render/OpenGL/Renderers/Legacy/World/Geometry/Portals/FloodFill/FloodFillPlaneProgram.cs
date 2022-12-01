using GlmSharp;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillPlaneProgram : RenderProgram
{
    public FloodFillPlaneProgram() : base("Flood fill plane")
    {
    }

    public void SetZ(float z) => Uniforms["z"] = z;
    public void SetMvp(mat4 mvp) => Uniforms["mvp"] = mvp;
    public void SetTexture(TextureUnit unit) => Uniforms["boundTexture"] = unit;

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec2 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;

        uniform float z;
        uniform mat4 mvp;

        void main()
        {
            uvFrag = uv;

            gl_Position = mvp * vec4(pos.xy, z, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform sampler2D boundTexture;

        void main()
        {
            fragColor = texture(boundTexture, uvFrag.st);
        }
    ";
}
