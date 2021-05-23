using Helion.Render.OpenGL.Legacy.Context;
using Helion.Render.OpenGL.Legacy.Context.Types;

namespace Helion.Render.OpenGL.Legacy.Shader.Component
{
    public class FragmentShaderComponent : ShaderComponent
    {
        public FragmentShaderComponent(IGLFunctions functions, string shaderText) : base(functions, shaderText)
        {
        }

        protected override ShaderComponentType GetShaderComponentType() => ShaderComponentType.Fragment;
    }
}