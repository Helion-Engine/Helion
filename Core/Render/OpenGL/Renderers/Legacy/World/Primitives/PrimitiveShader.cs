using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

public class PrimitiveShader : RenderProgram
{
    public PrimitiveShader() : base("Primitive")
    {
    }

    public void Mvp(mat4 mat) => Uniforms.Set(mat, "mvp");

    protected override string VertexShader => @"
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

    protected override string? FragmentShader => @"
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
