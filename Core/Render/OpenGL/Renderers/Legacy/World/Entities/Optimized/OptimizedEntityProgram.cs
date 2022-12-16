using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities.Optimized;

public class OptimizedEntityProgram : RenderProgram
{
    public OptimizedEntityProgram() : base("Entity (optimized)")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, "boundTexture");
    public void ViewRightNormal(Vec2F viewRightNormal) => Uniforms.Set(viewRightNormal, "viewRightNormal");
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, "mvp");

    protected override string? VertexShader => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float lightLevel;

        out float lightLevelOut;

        void main()
        {
            lightLevelOut = lightLevel;

            gl_Position = vec4(pos, 1);
        }
    ";

    protected override string? GeometryShader => @"
        #version 330 core

        layout(points) in;
        layout(triangle_strip, max_vertices = 4) out;

        in float lightLevelOut[];

        out vec2 uvFrag;
        flat out float lightLevelFrag;

        uniform mat4 mvp;
        uniform vec2 viewRightNormal;
        uniform sampler2D boundTexture;

        void main()
        {
            vec3 pos = gl_in[0].gl_Position.xyz;
            ivec2 textureDim = textureSize(boundTexture, 0);
            float halfTexWidth = textureDim.x * 0.5;
            vec3 posMoveDir = vec3(viewRightNormal, 0);
            vec3 minPos = pos - (posMoveDir * halfTexWidth);
            vec3 maxPos = pos + (posMoveDir * halfTexWidth) + (vec3(0, 0, 1) * textureDim.y);

            // Triangle strip ordering is: v0 v1 v2, v2 v1 v3
            // We also need to be going counter-clockwise.
            // Also the UV's are inverted, so draw from 1 down to 0 along the Y.

            gl_Position = mvp * vec4(minPos.x, minPos.y, minPos.z, 1);
            uvFrag = vec2(0, 1);
            lightLevelFrag = lightLevelOut[0];
            EmitVertex();

            gl_Position = mvp * vec4(maxPos.x, maxPos.y, minPos.z, 1);
            uvFrag = vec2(1, 1);
            lightLevelFrag = lightLevelOut[0];
            EmitVertex();

            gl_Position = mvp * vec4(minPos.x, minPos.y, maxPos.z, 1);
            uvFrag = vec2(0, 0);
            lightLevelFrag = lightLevelOut[0];
            EmitVertex();

            gl_Position = mvp * vec4(maxPos.x, maxPos.y, maxPos.z, 1);
            uvFrag = vec2(1, 0);
            lightLevelFrag = lightLevelOut[0];
            EmitVertex();
    
            EndPrimitive();
        }  
    ";

    protected override string? FragmentShader => @"
        #version 330

        in vec2 uvFrag;
        flat in float lightLevelFrag;

        out vec4 fragColor;

        uniform sampler2D boundTexture;

        float calculateLightLevel(float lightLevel)
        {
            if (lightLevel <= 0.75)
            {
                if (lightLevel > 0.4)
                {
	                lightLevel = -0.6375 + (1.85 * lightLevel);
	                if (lightLevel < 0.08)
                    {
		                lightLevel = 0.08 + (lightLevel * 0.2);
	                }
                } 
                else 
                {
	                lightLevel /= 5.0;
                }
            }

            return lightLevel;
        }

        void main()
        {
            float lightLevel = lightLevelFrag;  //calculateLightLevel(lightLevelFrag);

            fragColor = texture(boundTexture, uvFrag.st);
            fragColor.xyz *= lightLevel;

            if (fragColor.w <= 0.0)
                discard;
        }
    ";
}
