using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Shader.Component
{
    public class VertexShaderComponent : ShaderComponent
    {
        public VertexShaderComponent(IGLFunctions functions, string shaderText) : base(functions, shaderText)
        {
        }

        protected override ShaderComponentType GetShaderComponentType() => ShaderComponentType.Vertex;
    }
}