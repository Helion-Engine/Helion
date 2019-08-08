using Helion.Render.OpenGL.Old.Buffers;
using Helion.Render.OpenGL.Old.Shader;

namespace Helion.Render.OpenGL.Old.Renderers.World.Geometry.Dynamic.Flats
{
    public class DynamicFlatShaderProgram : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new UniformMatrix4();
        public readonly UniformInt TextureAtlas = new UniformInt();

        protected DynamicFlatShaderProgram(ShaderBuilder builder, VertexArrayAttributes attributes) : 
            base(builder, attributes)
        {
        }
        
        internal static DynamicFlatShaderProgram MakeShaderProgram(VertexArrayAttributes attributes)
        {
            ShaderBuilder shaderBuilder = new ShaderBuilder();
            
            shaderBuilder.VertexShaderText = @"
                #version 140

                in vec2 pos;
                in int textureTableIndex;
                in int planeIndex;

                out vec2 uvFrag;

                uniform mat4 mvp;

                void main() {
                    // TODO
                    uvFrag = vec2(0.0, 0.0);

                    gl_Position = mvp * vec4(pos, 1.0);
                }
            ";
            
            shaderBuilder.FragmentShaderText = @"
                #version 140

                in vec2 uvFrag;

                out vec4 fragColor;

                uniform sampler2D textureAtlas;

                void main() {
                    fragColor = texture(textureAtlas, uvFrag);
                }
            ";
            
            return new DynamicFlatShaderProgram(shaderBuilder, attributes);
        }
    }
}