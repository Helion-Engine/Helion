using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Modern.Renderers.World.Geometry
{
    public class ModernWorldGeometryShader : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new();

        protected override string VertexShader()
        {
            return @"
                #version 440 core

                #extension GL_NV_gpu_shader5 : require

                layout(location = 0) in vec3 pos;
                layout(location = 1) in vec2 uv;
                layout(location = 2) in uvec2 textureHandle;
                layout(location = 3) in vec4 rgbaScale;
                layout(location = 4) in float alpha;

                out vec2 uvFrag;
                out flat uvec2 textureHandleFrag;
                out flat vec3 rgbScaleFrag;
                out flat float alphaFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;
                    textureHandleFrag = textureHandle;
                    rgbScaleFrag = rgbaScale.xyz;
                    alphaFrag = alpha;
                    gl_Position = mvp * vec4(pos, 1);
                }
            ";
        }

        protected override string FragmentShader()
        {
            return @"
                #version 440 core

                #extension GL_ARB_bindless_texture : require
                #extension GL_ARB_shader_image_load_store : require
                #extension GL_ARB_shader_storage_buffer_object : require
                #extension GL_NV_gpu_shader5 : require

                // TODO: Can turn this on at the very end, need to make sure we don't violate discard rules though
                // layout(early_fragment_tests) in;

                in vec2 uvFrag;
                in flat uvec2 textureHandleFrag;
                in flat vec3 rgbScaleFrag;
                in flat float alphaFrag;

                out vec4 FragColor;

                void main() {
                    uint64_t handle = packUint2x32(textureHandleFrag);
                    sampler2D s = sampler2D(handle);

                    FragColor = texture(s, uvFrag);
                    FragColor *= vec4(rgbScaleFrag, 1);
                    FragColor.w *= alphaFrag;
                }
            ";
        }
    }
}
