using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Renderers.Hud
{
    public class GLHudShader : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new();
        
        protected override string VertexShader()
        {
            return @"";
        }

        protected override string FragmentShader()
        {
            return @"";
        }
    }
}
