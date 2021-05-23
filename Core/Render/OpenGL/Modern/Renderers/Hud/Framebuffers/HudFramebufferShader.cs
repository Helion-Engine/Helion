using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud.Framebuffers
{
    public class HudFramebufferShader : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new();
        public readonly UniformInt FramebufferTexture = new();
        
        protected override string VertexShader()
        {
            return @"
                #version 330 core

                layout(location = 0) in vec3 pos;
                layout(location = 1) in vec2 uv;

                out vec2 uvFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;
                    gl_Position = mvp * vec4(pos.xyz, 1);
                }
            ";
        }

        protected override string FragmentShader()
        {
            return @"
                #version 330 core

                in vec2 uvFrag;

                out vec4 FragColor;

                uniform sampler2D framebufferTexture;

                void main() {
                    FragColor = texture(framebufferTexture, uvFrag);
                }
            ";
        }
    }
}
