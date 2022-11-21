using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.World.Sky.Sphere;
using Helion.Render.Legacy.Shader;
using Helion.Render.Legacy.Shader.Component;
using Helion.Render.Legacy.Shader.Fields;
using Helion.Render.Legacy.Vertex;

namespace Helion.Render.Legacy.Renderers.World.Sky.Sphere;

public class SkySphereShader : ShaderProgram
{
    public readonly UniformInt BoundTexture = new();
    public readonly UniformInt HasInvulnerability = new();
    public readonly UniformMatrix4 Mvp = new();
    public readonly UniformFloat ScaleU = new();
    public readonly UniformInt FlipU = new();

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
            uniform int flipU;

            void main() {
                uvFrag = uv;
                if (flipU != 0) {
                    uvFrag.x = -uvFrag.x;
                }

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
