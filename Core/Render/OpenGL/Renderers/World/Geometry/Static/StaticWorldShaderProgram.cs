using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class StaticWorldShaderProgram : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new UniformMatrix4();
        public readonly UniformInt TextureAtlas = new UniformInt();
        public readonly UniformInt TextureInfoBuffer = new UniformInt();

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
                in vec2 localUV;
                in float lightLevel;
                in int textureInfoIndex;

                out vec2 localUVFrag;
                out float lightLevelFrag;
                flat out int textureInfoIndexFrag;

                uniform mat4 mvp;

                void main() {
                    localUVFrag = localUV;
                    lightLevelFrag = lightLevel;
                    textureInfoIndexFrag = textureInfoIndex;

                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";
            
            shaderBuilder.FragmentShaderText = @"
                #version 140

                const int TEXEL_OFFSET_FACTOR = 8;

                in vec2 localUVFrag;
                in float lightLevelFrag;
                flat in int textureInfoIndexFrag;

                out vec4 fragColor;

                uniform sampler2D textureAtlas;
                uniform samplerBuffer textureInfoBuffer;

                vec2 CalculateUV() {
                    int index = textureInfoIndexFrag * TEXEL_OFFSET_FACTOR;

                    float leftU = texelFetch(textureInfoBuffer, index).r;
                    float bottomV = texelFetch(textureInfoBuffer, index + 1).r;
                    float rightU = texelFetch(textureInfoBuffer, index + 2).r;
                    float topV = texelFetch(textureInfoBuffer, index + 3).r;
                    float widthU = rightU - leftU;
                    float heightV = topV - bottomV;

                    float offsetU = widthU * localUVFrag.x;
                    float offsetV = heightV * localUVFrag.y;

                    return vec2(leftU + offsetU, bottomV + offsetV);
                }

                void main() {
                    vec2 uv = CalculateUV();

                    fragColor = texture(textureAtlas, uv);
                    fragColor.xyz = fragColor.xyz * lightLevelFrag;

                    float tempValueProofOfConcept = texelFetch(textureInfoBuffer, 0).r;

                    if (fragColor.w <= 0.0)
                        discard;
                }
            ";
            
            return new StaticWorldShaderProgram(shaderBuilder, attributes);
        }
    }
}