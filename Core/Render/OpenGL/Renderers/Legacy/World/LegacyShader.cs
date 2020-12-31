using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shader.Component;
using Helion.Render.OpenGL.Shader.Fields;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    public class LegacyShader : ShaderProgram
    {
        public readonly UniformInt BoundTexture = new();
        public readonly UniformInt HasInvulnerability = new();
        public readonly UniformFloat LightLevelMix = new();
        public readonly UniformFloat LightLevelValue = new();
        public readonly UniformMatrix4 Mvp = new();
        public readonly UniformFloat TimeFrac = new();

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
                in float alpha;
                in vec3 colorMul;
                in float fuzz;

                out vec2 uvFrag;
                flat out float lightLevelFrag;
                flat out float alphaFrag;
                out vec3 colorMulFrag;
                flat out float fuzzFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;    
                    lightLevelFrag = clamp(lightLevel, 0.0, 1.0);
                    alphaFrag = alpha;
                    colorMulFrag = colorMul;
                    fuzzFrag = fuzz;

                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";

            const string fragmentShaderText = @"
                #version 130

                in vec2 uvFrag;
                flat in float lightLevelFrag;
                flat in float alphaFrag;
                in vec3 colorMulFrag;
                flat in float fuzzFrag;

                out vec4 fragColor;

                uniform int hasInvulnerability;
                uniform float timeFrac;
                uniform float lightLevelMix;
                uniform float lightLevelValue;
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
  
                    return mix(clamp(lightLevel, 0.0, 1.0), lightLevelValue, lightLevelMix);
                }

                // These two functions are found here:
                // https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83
                float rand(vec2 n) { 
	                return fract(sin(dot(n, vec2(12.9898, 4.1414))) * 43758.5453);
                }

                float noise(vec2 p) {
	                vec2 ip = floor(p);
	                vec2 u = fract(p);
	                u = u * u * (3.0 - 2.0 * u);
	                
	                float res = mix(
		                mix(rand(ip), rand(ip + vec2(1.0, 0.0)), u.x),
		                mix(rand(ip + vec2(0.0, 1.0)), rand(ip + vec2(1.0, 1.0)), u.x), u.y);
	                return res * res;
                }

                void main() {
                    fragColor = texture(boundTexture, uvFrag.st);
                    fragColor.xyz *= colorMulFrag;
                    fragColor.xyz *= calculateLightLevel();
                    fragColor.w *= alphaFrag;

                    if (fuzzFrag > 0) {
                        // The division/floor is to chunk pixels together to make
                        // blocks. A larger denominator makes it more blocky.
                        vec2 blockCoordinate = floor(gl_FragCoord.xy / 2);

                        // I chose 0.3 because it gave the best ratio if alpha to non-alpha.
                        fragColor.w *= step(0.3, noise(blockCoordinate * timeFrac));
                    }

                    if (fragColor.w <= 0.0)
                        discard;

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

            VertexShaderComponent vertexShaderComponent = new(functions, vertexShaderText);
            FragmentShaderComponent fragmentShaderComponent = new(functions, fragmentShaderText);
            return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
        }
    }
}