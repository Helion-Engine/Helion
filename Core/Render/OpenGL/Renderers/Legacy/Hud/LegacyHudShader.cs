using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shader.Component;
using Helion.Render.OpenGL.Shader.Fields;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    public class LegacyHudShader : ShaderProgram
    {
        public readonly UniformInt BoundTexture = new UniformInt();
        public readonly UniformMatrix4 Mvp = new UniformMatrix4();

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
                in vec4 rgbMix;
                in vec4 rgbMultiplier;    
                in float alpha;

                out vec2 uvFrag;
                flat out vec4 rgbMixFrag;
                flat out vec4 rgbMultiplierFrag;   
                flat out float alphaFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;
                    rgbMixFrag = rgbMix;
                    rgbMultiplierFrag = rgbMultiplier; 
                    alphaFrag = alpha; 

                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";
            
            const string fragmentShaderText = @"
                #version 130

                in vec2 uvFrag;
                flat in vec4 rgbMixFrag;
                flat in vec4 rgbMultiplierFrag;   
                flat in float alphaFrag;

                out vec4 fragColor;

                uniform sampler2D boundTexture;

                void main() {
                    fragColor = texture(boundTexture, uvFrag.st);
                    fragColor.w *= alphaFrag;

                    fragColor.xyz = mix(fragColor.xyz, rgbMixFrag.xyz, rgbMixFrag.w);
                    fragColor.xyz *= mix(vec3(1.0, 1.0, 1.0), rgbMultiplierFrag.xyz, rgbMultiplierFrag.w);
                }
            ";
            
            VertexShaderComponent vertexShaderComponent = new VertexShaderComponent(functions, vertexShaderText);
            FragmentShaderComponent fragmentShaderComponent = new FragmentShaderComponent(functions, fragmentShaderText);
            return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
        }
    }
}