using GlmSharp;
using Helion.Render.OpenGL.Shader;

namespace Helion.UI.Shaders.GlowingMap;

public class GlowingMapLineProgram : RenderProgram
{
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, "mvp");
    public void RootDistance(float rootDistance) => Uniforms.Set(rootDistance, "rootDistance");
    public void OffsetZ(float z) => Uniforms.Set(z, "offsetZ");
    
    public GlowingMapLineProgram() : base("Glowing map line")
    {
    }

    protected override string VertexShader()
    {
        return @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in int numSides;
        layout(location = 2) in float rootDistance;

        flat out int numSidesFrag;
        out float rootDistanceFrag;

        uniform mat4 mvp;
        uniform float offsetZ;

        void main()
        {
            numSidesFrag = numSides;
            rootDistanceFrag = rootDistance;

            vec3 position = vec3(pos.xy, pos.z + offsetZ);
            gl_Position = mvp * vec4(position, 1);
        }
        ";
    }

    protected override string FragmentShader()
    {
        return @"
        #version 330

        flat in int numSidesFrag;
        in float rootDistanceFrag;
        
        out vec4 fragColor;

        uniform float rootDistance;

        void main()
        {
            if (rootDistanceFrag <= rootDistance) {
                if (numSidesFrag == 1) {
                    fragColor = vec4(0.0, 1.0, 1.0, 1.0);
                } else {
                    fragColor = vec4(0.0, 0.5, 0.5, 1.0);
                }
            } else {
                //fragColor = vec4(0.4, 0.4, 0.4, 0.4);
                discard;
            }
        }
        ";
    }
}