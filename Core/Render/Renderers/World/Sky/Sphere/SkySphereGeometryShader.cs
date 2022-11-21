using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Shader;
using Helion.Render.Legacy.Shader.Component;
using Helion.Render.Legacy.Shader.Fields;
using Helion.Render.Legacy.Vertex;
using Helion.Render.Renderers.World.Sky.Sphere;

namespace Helion.Render.Renderers.World.Sky.Sphere;

public class SkySphereGeometryShader : ShaderProgram
{
    public readonly UniformMatrix4 Mvp = new UniformMatrix4();

    public SkySphereGeometryShader(IGLFunctions functions, ShaderBuilder builder, VertexArrayAttributes attributes) :
        base(functions, builder, attributes)
    {
    }

    public static ShaderBuilder MakeBuilder(IGLFunctions functions)
    {
        const string vertexShaderText = @"
            #version 130

            in vec3 pos;

            uniform mat4 mvp;

            void main() {
                gl_Position = mvp * vec4(pos, 1.0);
            }
        ";

        const string fragmentShaderText = @"
            #version 130

            out vec4 fragColor;

            void main() {
                fragColor = vec4(1.0, 1.0, 1.0, 1.0);
            }
        ";

        VertexShaderComponent vertexShaderComponent = new VertexShaderComponent(functions, vertexShaderText);
        FragmentShaderComponent fragmentShaderComponent = new FragmentShaderComponent(functions, fragmentShaderText);
        return new ShaderBuilder(vertexShaderComponent, fragmentShaderComponent);
    }
}
