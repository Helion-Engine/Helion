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
        public readonly UniformInt HasInvulnerability = new UniformInt();
        public readonly UniformMatrix4 Mvp = new UniformMatrix4();
        public readonly UniformFloat ScaleU = new UniformFloat();

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

            VertexShaderComponent vertexShaderComponent = new VertexShaderComponent(functions, vertexShaderText);
            FragmentShaderComponent fragmentShaderComponent = new FragmentShaderComponent(functions, fragmentShaderText);
            return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
        }
    }
}