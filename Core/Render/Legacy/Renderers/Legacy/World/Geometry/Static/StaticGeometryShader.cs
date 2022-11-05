using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Shader.Component;
using Helion.Render.Legacy.Shader;
using Helion.Render.Legacy.Vertex;
using Helion.Render.Legacy.Shader.Fields;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Geometry.Static;

public class StaticGeometryShader : ShaderProgram
{
    public readonly UniformInt BoundTexture = new();
    public readonly UniformMatrix4 Mvp = new();

    public StaticGeometryShader(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes) :
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
            flat out float lightLevelFrag;

            uniform mat4 mvp;

            void main() {
                uvFrag = uv;
                lightLevelFrag = clamp(lightLevel, 0.0, 256.0);

                gl_Position = mvp * vec4(pos, 1.0);
            }
        ";

        const string fragmentShaderText = @"
            #version 130

            in vec2 uvFrag;
            flat in float lightLevelFrag;

            out vec4 fragColor;

            uniform sampler2D boundTexture;

            void main() {
                fragColor = texture(boundTexture, uvFrag.st);

                float lightLevel = lightLevelFrag / 256.0;
                fragColor.xyz *= lightLevel;

                if (fragColor.w <= 0.0)
                    discard;
            }
        ";

        VertexShaderComponent vertexShaderComponent = new(functions, vertexShaderText);
        FragmentShaderComponent fragmentShaderComponent = new(functions, fragmentShaderText);
        return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
    }

}
