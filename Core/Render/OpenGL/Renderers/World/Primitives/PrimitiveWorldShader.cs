using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Renderers.World.Primitives
{
    public class PrimitiveWorldShader : ShaderProgram
    {
        public readonly UniformMatrix4 mvp = new();
        
        protected override string VertexShader()
        {
            return @"
                #version 110

                attribute vec3 pos;
                attribute vec4 color;

                varying vec4 colorFrag;

                uniform mat4 mvp;

                void main() {
                    gl_Position = mvp * vec4(pos.x, pos.y, pos.z, 1.0);
                    colorFrag = color;
                }
            ";
        }

        protected override string FragmentShader()
        {
            return @"
                #version 110

                varying vec4 colorFrag;

                void main() {
                    gl_FragColor = colorFrag;
                }
            ";
        }
    }
}
