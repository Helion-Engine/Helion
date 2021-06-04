using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Shader.Component
{
    public class FragmentShaderComponent : ShaderComponent
    {
        public FragmentShaderComponent(IGLFunctions functions, string shaderText) : base(functions, shaderText)
        {
        }

        protected override ShaderComponentType GetShaderComponentType() => ShaderComponentType.Fragment;
    }
}