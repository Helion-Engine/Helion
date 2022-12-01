using GlmSharp;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

/// <summary>
/// A simple program designed just to render primitives so that the stencil
/// buffer gets populated.
/// </summary>
public class PortalStencilProgram : RenderProgram
{
    public PortalStencilProgram() : base("Portal stencil")
    {
    }

    public void SetMvp(mat4 mvp) => Uniforms["mvp"] = mvp;

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;

        uniform mat4 mvp;

        void main()
        {
            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        out vec4 fragColor;

        void main()
        {
            fragColor = vec4(1, 1, 1, 1);
        }
    ";
}
