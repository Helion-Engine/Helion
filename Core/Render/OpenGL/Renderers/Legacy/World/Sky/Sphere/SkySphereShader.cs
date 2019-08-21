using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shader.Component;
using Helion.Render.OpenGL.Shader.Fields;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere
{
    public class SkySphereShader : ShaderProgram
    {
        public readonly UniformInt BoundTexture = new UniformInt();
        public readonly UniformMatrix4 Mvp = new UniformMatrix4();

        public SkySphereShader(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes) : 
            base(functions, builder, attributes)
        {
        }

        public static ShaderBuilder MakeBuilder(IGLFunctions functions)
        {
            const string vertexShaderText = @"
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
            
            const string fragmentShaderText = @"
                #version 130

                in vec2 uvFrag;

                out vec4 fragColor;

                uniform sampler2D boundTexture;

                void main() {
                    fragColor = texture(boundTexture, uvFrag.st);
                }
            ";
            
            VertexShaderComponent vertexShaderComponent = new VertexShaderComponent(functions, vertexShaderText);
            FragmentShaderComponent fragmentShaderComponent = new FragmentShaderComponent(functions, fragmentShaderText);
            return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
        }
    }
}