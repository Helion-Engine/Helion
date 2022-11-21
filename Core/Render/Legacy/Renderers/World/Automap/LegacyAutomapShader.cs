using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.World.Automap;
using Helion.Render.Legacy.Shader;
using Helion.Render.Legacy.Shader.Component;
using Helion.Render.Legacy.Shader.Fields;
using Helion.Render.Legacy.Vertex;

namespace Helion.Render.Legacy.Renderers.World.Automap;

public class LegacyAutomapShader : ShaderProgram
{
    public readonly UniformVec3 Color = new();
    public readonly UniformMatrix4 Mvp = new();

    public LegacyAutomapShader(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes) :
        base(functions, builder, attributes)
    {
    }

    public static ShaderBuilder MakeBuilder(IGLFunctions functions)
    {
        const string vertexShaderText = @"
            #version 130

            in vec2 pos;

            uniform mat4 mvp;

            void main() {
                gl_Position = mvp * vec4(pos, 0.5, 1.0);
            }
        ";

        const string fragmentShaderText = @"
            #version 130

            out vec4 fragColor;

            uniform vec3 color;

            void main() {
                fragColor = vec4(color, 1.0f);
            }
        ";

        VertexShaderComponent vertexShaderComponent = new(functions, vertexShaderText);
        FragmentShaderComponent fragmentShaderComponent = new(functions, fragmentShaderText);
        return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
    }
}
