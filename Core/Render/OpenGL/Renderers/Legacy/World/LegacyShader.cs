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
        public readonly UniformMatrix4 Mvp = new();
        public readonly UniformFloat TimeFrac = new();
        public readonly UniformVec3 Camera = new();
        public readonly UniformFloat LookingAngle = new();
        public readonly UniformFloat LightLevelMix = new();
        public readonly UniformFloat LightLevelValue = new();

        public LegacyShader(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes) :
            base(functions, builder, attributes)
        {
        }

        public static ShaderBuilder MakeBuilder(IGLFunctions functions)
        {
            const string vertexShaderText = @"
                #version 130

                in vec3 pos;
                in vec3 looking;
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
                out vec2 posFrag;
                
                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;    
                    lightLevelFrag = clamp(lightLevel, 0.0, 1.0);
                    alphaFrag = alpha;
                    colorMulFrag = colorMul;
                    fuzzFrag = fuzz;

                    gl_Position = mvp * vec4(pos, 1.0);
                    posFrag = pos.xy;
                }
            ";

            const string fragmentShaderText = @"
                #version 130

                in vec2 uvFrag;
                flat in float lightLevelFrag;
                flat in float alphaFrag;
                in vec3 colorMulFrag;
                flat in float fuzzFrag;
                in vec2 posFrag;

                out vec4 fragColor;

                uniform int hasInvulnerability;
                uniform float timeFrac;
                uniform sampler2D boundTexture;
                uniform vec3 camera;
                uniform float lookingAngle;
                uniform float lightLevelMix;
                uniform float lightLevelValue;

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

                const float halfPi = 1.57079632679489661923;

                // Defined in GLHelper as well
                const int colorMaps = 32;
                const int colorMapClamp = 31;
                const int scaleCount = 16;
                const int maxLightScale = 23;
                const int lightFadeStart = 56;

                int getLightLevelAdd(float d)
                {
                    d = clamp(d - lightFadeStart, 0, d);                  
                    return int(21.53536 + (-0.09935881 - 21.53536)/(1 + pow((d/48.46036), 0.9737408)));
                }

                int getLightLevelIndex(float lightLevel, int add)
                {
                    int index = clamp(int(lightLevel * 256 / scaleCount), 0, scaleCount - 1);
                    int startMap = (scaleCount - index - 1) * 2 * colorMaps/scaleCount;
                    add = maxLightScale - clamp(add, 0, maxLightScale);
                    return clamp(startMap - add, 0, colorMapClamp);
                }

                void main() {
                    float lightLevel = lightLevelFrag;
                    float c = distance(posFrag, camera.xy);
                    float angle = halfPi + atan(posFrag.y - camera.y, posFrag.x - camera.x) - lookingAngle;
                    float a = c * sin(angle);

                    int index = getLightLevelIndex(lightLevel, getLightLevelAdd(a));
                    lightLevel = float(colorMaps - index) / colorMaps;
                    lightLevel = mix(clamp(lightLevel, 0.0, 1.0), lightLevelValue, lightLevelMix);

                    fragColor = texture(boundTexture, uvFrag.st);
                    fragColor.xyz *= colorMulFrag;
                    fragColor.xyz *= lightLevel;
                    fragColor.w *= alphaFrag;

                    if (fuzzFrag > 0) {
                        // The division/floor is to chunk pixels together to make
                        // blocks. A larger denominator makes it more blocky.
                        vec2 blockCoordinate = floor(gl_FragCoord.xy);

                        // I chose 0.3 because it gave the best ratio if alpha to non-alpha.
                        fragColor.w *= step(0.25, noise(blockCoordinate * timeFrac));
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