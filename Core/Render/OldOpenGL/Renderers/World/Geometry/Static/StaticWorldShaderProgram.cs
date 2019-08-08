using Helion.Render.OpenGL.Old.Buffers;
using Helion.Render.OpenGL.Old.Shader;

namespace Helion.Render.OpenGL.Old.Renderers.World.Geometry.Static
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

                const int TEXEL_OFFSET_FACTOR = 8;

                in vec3 pos;
                in vec2 localUV;
                in float lightLevel;
                in int textureInfoIndex;

                out vec2 uvFrag;
                out float lightLevelFrag;
                flat out vec2 uvDimension;
                flat out vec2 uvOffset;

                uniform mat4 mvp;
                uniform samplerBuffer textureInfoBuffer;

                void CalculateUV() {
                    int index = textureInfoIndex * TEXEL_OFFSET_FACTOR;

                    // TODO: Would it be better to sample this as a vec4?
                    float leftU = texelFetch(textureInfoBuffer, index).r;
                    float bottomV = texelFetch(textureInfoBuffer, index + 1).r;
                    float rightU = texelFetch(textureInfoBuffer, index + 2).r;
                    float topV = texelFetch(textureInfoBuffer, index + 3).r;
                    float widthU = rightU - leftU;
                    float heightV = topV - bottomV;

                    uvDimension = vec2(widthU, heightV);
                    uvOffset = vec2(leftU, bottomV);
                }

                void main() {
                    CalculateUV();

                    uvFrag = localUV;
                    lightLevelFrag = lightLevel;

                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";
            
            shaderBuilder.FragmentShaderText = @"
                #version 140

                in vec2 uvFrag;
                in float lightLevelFrag;
                flat in vec2 uvDimension;
                flat in vec2 uvOffset;

                out vec4 fragColor;

                uniform sampler2D textureAtlas;

                void main() {
                    float u = mod((uvFrag.x * uvDimension.x), uvDimension.x) + uvOffset.x;
                    float v = mod((uvFrag.y * uvDimension.y), uvDimension.y) + uvOffset.y;

                    fragColor = texture(textureAtlas, vec2(u, v));
                    fragColor.xyz = fragColor.xyz * lightLevelFrag;

                    if (fragColor.w <= 0.0)
                        discard;
                }
            ";
            
            return new StaticWorldShaderProgram(shaderBuilder, attributes);
        }
    }
}