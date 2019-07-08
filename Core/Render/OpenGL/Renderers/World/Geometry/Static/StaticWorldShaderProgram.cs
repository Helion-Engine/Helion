using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class StaticWorldShaderProgram : ShaderProgram
    {
        public StaticWorldShaderProgram(ShaderBuilder builder, VertexArrayAttributes attributes) : 
            base(builder, attributes)
        {
        }

        internal static StaticWorldShaderProgram MakeShaderProgram(VertexArrayAttributes attributes)
        {
            ShaderBuilder shaderBuilder = new ShaderBuilder();
            
            shaderBuilder.VertexShaderText = @"
                #version 140

                in vec2 pos;
                in float u;
                in int floorPlaneIndex;
                in int ceilingPlaneIndex;
                in int wallIndex;
                in int flags;

                out vec2 uvFrag;

                void main() {
                    int z = floorPlaneIndex + ceilingPlaneIndex + wallIndex + flags;

                    uvFrag = vec2(u, z);
                    gl_Position = vec4(pos, z, 1.0);
                }
            ";
            
            shaderBuilder.FragmentShaderText = @"
                #version 140

                in vec2 uvFrag;

                out vec4 fragColor;

                void main() {
                    fragColor = vec4(uvFrag.x, uvFrag.y, 0.0, 1.0);
                }
            ";
            
            return new StaticWorldShaderProgram(shaderBuilder, attributes);
        }
    }
}