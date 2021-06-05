using Helion.Render.Legacy.Shader.Fields;
using Helion.Render.OpenGL.Shaders;

namespace Helion.Render.OpenGL.Renderers.World.Images
{
    public class GLImageWorldShader : ShaderProgram
    {
        public readonly UniformInt BoundTexture = new();
        public readonly UniformMatrix4 Mvp = new();
        
        protected override string VertexShader()
        {
            return @"
                #version 110

                attribute vec3 pos;
                attribute vec2 uv;
                attribute vec4 color;

                varying vec2 uvFrag;
                varying vec4 colorFrag;

                uniform mat4 mvp;

                void main() {
                    gl_Position = mvp * vec4(pos.x, pos.y, pos.z, 1.0);
                    uvFrag = uv;
                    colorFrag = color;
                }
            ";
        }

        protected override string FragmentShader()
        {
            return @"
                #version 110

                varying vec2 uvFrag;
                varying vec4 colorFrag;

                uniform sampler2D boundTexture;

                void main() {
                    gl_FragColor = texture2D(boundTexture, uvFrag) * colorFrag;
                }
            ";
        }
    }
}
