using Helion.Render.OpenGL.Shared.Buffer.Vao;
using Helion.Render.OpenGL.Shared.Shader;
using Helion.Util;

namespace Helion.Render.OpenGL.Legacy.Renderers.Console
{
    public static class ConsoleShader
    {
        public static ShaderProgram CreateShaderProgramOrThrow(VertexArrayObject? vao = null)
        {
            ShaderBuilder builder = new ShaderBuilder();

            builder.VertexShaderText = @"
                #version 130
                
                in vec2 pos;
                in vec2 uv;

                out vec2 uvFrag;

                void main() {
                    uvFrag = uv;
                    gl_Position = vec4(pos.x, pos.y, 0.0, 1.0);
                }
            ";

            builder.FragmentShaderText = @"
                #version 130

                in vec2 uvFrag;

                out vec4 fragColor;

                uniform sampler2D boundTexture;

                void main() {
                    fragColor = texture(boundTexture, uvFrag);
                }
            ";

            ShaderProgram? program = ShaderProgram.Create(builder, vao);
            if (program == null)
                throw new HelionException("Unexpected failure when creating console shader");
            return program;
        }
    }
}
