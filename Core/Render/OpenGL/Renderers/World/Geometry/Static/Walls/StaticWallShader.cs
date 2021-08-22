using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls
{
    public class StaticWallShader : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new();
        public readonly UniformTexture Tex = new();
        public readonly UniformTexture Data = new();

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
                uniform sampler2D data;

                void main() {
                    gl_FragColor = texture2D(tex, uvFrag);
                    gl_FragColor += texture2D(data, uvFrag);
                }
            ";
        }
    }
}
