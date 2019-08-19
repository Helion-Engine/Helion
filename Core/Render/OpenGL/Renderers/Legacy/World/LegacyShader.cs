using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shader.Component;
using Helion.Render.OpenGL.Shader.Fields;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    public class LegacyShader : ShaderProgram
    {
        public readonly UniformInt BoundTexture = new UniformInt();
        public readonly UniformMatrix4 Mvp = new UniformMatrix4();

        public LegacyShader(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes) : 
            base(functions, builder, attributes)
        {
        }

        public static ShaderBuilder MakeBuilder(IGLFunctions functions)
        {
            const string vertexShaderText = @"
                #version 130

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
            
            const string fragmentShaderText = @"
                #version 130

                in vec2 uvFrag;
                in float lightLevelFrag;

                out vec4 fragColor;

                uniform sampler2D boundTexture;

                float calculateLightLevel() {
                    float lightLevel = lightLevelFrag;

                    if (lightLevel <= 0.75) {
	                    if (lightLevel > 0.4) {
		                    lightLevel = -0.6375 + (1.85 * lightLevel);
		                    if (lightLevel < 0.08) {
			                    lightLevel = 0.08 + (lightLevel * 0.2);
		                    }
	                    } else {
		                    lightLevel /= 5.0;
	                    }
                    }
  
                    return clamp(lightLevel, 0.0, 1.0);
                }

                void main() {
                    fragColor = texture(boundTexture, uvFrag.st);
                    fragColor.xyz *= calculateLightLevel();

                    if (fragColor.w <= 0.0)
                        discard;
                }
            ";
            
            VertexShaderComponent vertexShaderComponent = new VertexShaderComponent(functions, vertexShaderText);
            FragmentShaderComponent fragmentShaderComponent = new FragmentShaderComponent(functions, fragmentShaderText);
            return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
        }
    }
}