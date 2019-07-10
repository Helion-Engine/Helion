using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class StaticWorldShaderProgram : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new UniformMatrix4();
        public readonly UniformInt TextureAtlas = new UniformInt();

        public StaticWorldShaderProgram(ShaderBuilder builder, VertexArrayAttributes attributes) : 
            base(builder, attributes)
        {
        }

        internal static StaticWorldShaderProgram MakeShaderProgram(VertexArrayAttributes attributes)
        {
            ShaderBuilder shaderBuilder = new ShaderBuilder();
            
            shaderBuilder.VertexShaderText = @"
                #version 140

                in vec3 pos;
                in vec2 uv;
                in float lightLevel;

                out vec2 uvFrag;
                out float lightLevelFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;
                    lightLevelFrag = lightLevel;

                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";
            
            shaderBuilder.FragmentShaderText = @"
                #version 140

                in vec2 uvFrag;
                in float lightLevelFrag;

                out vec4 fragColor;

                uniform sampler2D textureAtlas;

                void main() {
                    // TODO: Clamp uvFrag to [0.0, 1.0]

                    fragColor = texture(textureAtlas, uvFrag);
                    fragColor.xyz = fragColor.xyz * lightLevelFrag;

                    if (fragColor.w <= 0.0)
                        discard;
                }
            ";
            
            return new StaticWorldShaderProgram(shaderBuilder, attributes);
        }
    }
}