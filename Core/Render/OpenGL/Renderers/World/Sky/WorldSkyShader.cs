using Helion.Render.OpenGL.Buffer.Vao;
using Helion.Render.OpenGL.Shader;
using Helion.Util;

namespace Helion.Render.OpenGL.Renderers.World.Sky
{
    public static class WorldSkyShader
    {
        public static ShaderProgram CreateSkyGeometryShaderProgramOrThrow(VertexArrayObject? vao = null)
        {
            ShaderBuilder builder = new ShaderBuilder();

            builder.VertexShaderText = @"
                #version 130
                
                in vec3 pos;

                uniform mat4 mvp;

                void main() {
                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";

            builder.FragmentShaderText = @"
                #version 130

                out vec4 fragColor;

                void main() {
                    fragColor = vec4(0.0, 0.0, 0.0, 1.0);
                }
            ";

            ShaderProgram? program = ShaderProgram.Create(builder, vao);
            if (program == null)
                throw new HelionException("Unexpected failure when creating world shader");
            return program;
        }
        
        public static ShaderProgram CreateSkyboxShaderProgramOrThrow(VertexArrayObject? vao = null)
        {
            ShaderBuilder builder = new ShaderBuilder();

            builder.VertexShaderText = @"
                #version 130
                
                in vec3 pos;
                in vec2 uv;

                out vec2 uvFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;
                    gl_Position = mvp * vec4(pos, 1.0);
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
                throw new HelionException("Unexpected failure when creating world shader");
            return program;
        }
    }
}