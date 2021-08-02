using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Renderers.Hud
{
    public class GLHudTextureShader : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new();
        public readonly UniformInt Tex = new();
        
        protected override string VertexShader()
        {
            return @"
                #version 110

                attribute vec3 pos;
                attribute vec2 uv;

                varying vec2 uvFrag;

                uniform mat4 mvp;

                void main() {    
                    gl_Position = mvp * vec4(pos.x, pos.y, pos.z, 1.0);
                    uvFrag = uv;
                }
            ";
        }

        protected override string FragmentShader()
        {
            return @"
                #version 110

                varying vec2 uvFrag;

                uniform sampler2D tex;

                void main() {
                    gl_FragColor = texture2D(tex, uvFrag);
                }
            ";
        }
    }
}
