using Helion.Render.OpenGL.Legacy.Context;
using Helion.Render.OpenGL.Legacy.Shader;
using Helion.Render.OpenGL.Legacy.Shader.Component;
using Helion.Render.OpenGL.Legacy.Shader.Fields;
using Helion.Render.OpenGL.Legacy.Vertex;

namespace Helion.Render.OpenGL.Legacy.Renderers.Legacy.Hud
{
    public class LegacyHudShader : ShaderProgram
    {
        public readonly UniformInt BoundTexture = new();
        public readonly UniformMatrix4 Mvp = new();

        public LegacyHudShader(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes) :
            base(functions, builder, attributes)
        {
        }

        public static ShaderBuilder MakeBuilder(IGLFunctions functions)
        {
            const string vertexShaderText = @"
                #version 130

                in vec3 pos;
                in vec2 uv;
                in vec4 rgbMultiplier;    
                in float alpha;
                in float hasInvulnerability;

                out vec2 uvFrag;
                flat out vec4 rgbMultiplierFrag;   
                flat out float alphaFrag;
                flat out float hasInvulnerabilityFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;
                    rgbMultiplierFrag = rgbMultiplier; 
                    alphaFrag = alpha; 
                    hasInvulnerabilityFrag = hasInvulnerability;

                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";

            const string fragmentShaderText = @"
                #version 130

                in vec2 uvFrag;
                flat in vec4 rgbMultiplierFrag;   
                flat in float alphaFrag;
                flat in float hasInvulnerabilityFrag;

                out vec4 fragColor;

                uniform sampler2D boundTexture;

                void main() {
                    fragColor = texture(boundTexture, uvFrag.st);
                    fragColor.w *= alphaFrag;
                    fragColor.xyz *= mix(vec3(1.0, 1.0, 1.0), rgbMultiplierFrag.xyz, rgbMultiplierFrag.w);

                    if (hasInvulnerabilityFrag != 0) {
                        float maxColor = max(max(fragColor.x, fragColor.y), fragColor.z);
                        maxColor *= 1.5;
                        fragColor.xyz = vec3(maxColor, maxColor, maxColor);
                    }
                }
            ";

            VertexShaderComponent vertexShaderComponent = new(functions, vertexShaderText);
            FragmentShaderComponent fragmentShaderComponent = new(functions, fragmentShaderText);
            return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
        }
    }
}