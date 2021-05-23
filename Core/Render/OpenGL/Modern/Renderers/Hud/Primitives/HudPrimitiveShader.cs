using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud.Primitives
{
    public class HudPrimitiveShader : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new();
        
        protected override string VertexShader()
        {
            return @"
                #version 330 core

                layout(location = 0) in vec3 pos;
                layout(location = 1) in vec4 rgba;

                out vec4 fragRgba;

                uniform mat4 mvp;

                void main() {
                    fragRgba = rgba;
                    gl_Position = mvp * vec4(pos.xyz, 1);
                }
            ";
        }

        protected override string FragmentShader()
        {
            return @"
                #version 330 core

                in vec4 fragRgba;

                out vec4 FragColor;

                void main() {
                    FragColor = fragRgba;
                }
            ";
        }
    }
}
