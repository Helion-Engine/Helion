using Helion.Render.OpenGL.Legacy.Context;
using Helion.Render.OpenGL.Legacy.Context.Types;

namespace Helion.Render.OpenGL.Legacy.Shader.Component
{
    public class VertexShaderComponent : ShaderComponent
    {
        public VertexShaderComponent(IGLFunctions functions, string shaderText) : base(functions, shaderText)
        {
        }

        protected override ShaderComponentType GetShaderComponentType() => ShaderComponentType.Vertex;
    }
}