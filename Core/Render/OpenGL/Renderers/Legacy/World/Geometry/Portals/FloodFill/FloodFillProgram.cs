using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillProgram : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_cameraLocation;
    private readonly int m_mvpLocation;
    private readonly int m_timeFracLocation;

    public FloodFillProgram() : base("Flood fill plane")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_cameraLocation = Uniforms.GetLocation("camera");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_timeFracLocation = Uniforms.GetLocation("timeFrac");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void Camera(Vec3F camera) => Uniforms.Set(camera, m_cameraLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    public void TimeFrac(float frac) => Uniforms.Set(frac, m_timeFracLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float planeZ;
        layout(location = 2) in float minViewZ;
        layout(location = 3) in float maxViewZ;
        layout(location = 4) in float prevZ;
        layout(location = 5) in float prevPlaneZ;

        flat out float planeZFrag;
        out vec3 vertexPosFrag;

        uniform mat4 mvp;
        uniform vec3 camera;
        uniform float timeFrac;

        void main()
        {
            vec3 prevPos = vec3(pos.x, pos.y, prevZ);
            planeZFrag = mix(prevPlaneZ, planeZ, timeFrac);
            vertexPosFrag = mix(prevPos, pos, timeFrac);

            if (camera.z <= minViewZ || camera.z >= maxViewZ)
                gl_Position = vec4(0, 0, 0, 1);
            else
                gl_Position = mvp * vec4(vertexPosFrag, 1.0); 
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        flat in float planeZFrag;
        in vec3 vertexPosFrag;

        out vec4 fragColor;

        uniform sampler2D boundTexture;
        uniform vec3 camera;

        vec2 calcPlaneUV()
        {
            vec3 planeNormal = vec3(0, 0, 1);
            vec3 pointOnPlane = vec3(0, 0, planeZFrag);
            vec3 lookDir = normalize(vertexPosFrag - camera);
            float d = dot(pointOnPlane - camera, planeNormal) / dot(lookDir, planeNormal);
            vec3 planePos = camera + (lookDir * d);
            vec2 texDim = textureSize(boundTexture, 0);
            return vec2(planePos.x / texDim.x, planePos.y / texDim.y);
        }

        void main()
        {
            vec2 uv = calcPlaneUV();
            fragColor = texture(boundTexture, uv);
        }
    ";
}
