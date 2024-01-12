using GlmSharp;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

public class PrimitiveShader : RenderProgram
{
    private readonly int m_mvpLocation;

    public PrimitiveShader() : base("Primitive")
    {
        m_mvpLocation = Uniforms.GetLocation("mvp");
    }

    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec3 rgb;
        layout(location = 2) in float alpha;

        flat out vec3 rgbFrag;
        flat out float alphaFrag;

        uniform mat4 mvp;

        void main() 
        {
            rgbFrag = rgb;
            alphaFrag = alpha;

            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        flat in vec3 rgbFrag;
        flat in float alphaFrag;

        out vec4 fragColor;

        void main() 
        {
            fragColor = vec4(rgbFrag, alphaFrag);
        }
    ";
}
