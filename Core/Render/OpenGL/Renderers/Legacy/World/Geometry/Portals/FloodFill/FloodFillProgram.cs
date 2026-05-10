using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillProgram : RenderProgramBase
{
    private readonly int m_cameraLocation;
    private readonly int m_cameraDirection;

    public FloodFillProgram(string name) : base($"FloodFill - {name}")
    {
        m_cameraLocation = Uniforms.GetLocation("camera");
        m_cameraDirection = Uniforms.GetLocation("cameraDirection");
    }

    public void CameraDirection(Vec3F dir) => ProgramUniforms.Set(dir, m_cameraDirection);
    public void Camera(Vec3F camera) => ProgramUniforms.Set(camera, m_cameraLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float planeZ;
        layout(location = 2) in float minViewZ;
        layout(location = 3) in float maxViewZ;
        layout(location = 4) in float prevZ;
        layout(location = 5) in float prevPlaneZ;
        layout(location = 6) in float surfaceOptions;
        layout(location = 7) in float renderOptions;
        layout(location = 8) in float mapId;

        flat out float planeZFrag;
        out vec3 vertexPosFrag;
        out float dist2D;
        flat out float distanceOffsetFrag;
        flat out float colorMapIndexFrag;
        flat out float uvFlags;
        flat out float vertexLightLevelFrag;
        flat out float mapIdFrag;
        flat out float upperFrag;
        flat out float lowerFrag;
        out float depthFrag;

        ${SectorColorMapVertexFragVariables}
        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}
        ${SectorColorMapVertexUniformVariables}

        uniform mat4 mvp;
        uniform vec3 camera;
        uniform vec3 cameraDirection;
        uniform float timeFrac;
        uniform int useSectorColor;
        uniform int useSectorFog;

        void main()
        {
            vec3 prevPos = vec3(pos.x, pos.y, prevZ);
            planeZFrag = mix(prevPlaneZ, planeZ, timeFrac);
            vertexPosFrag = mix(prevPos, pos, timeFrac);
            mapIdFrag = mapId;

            float alphaFrag;
            float addAlphaFrag;
            ${VertexOptionsSet}
            ${ColorMapAndLightLevelSet}

            ${SectorColorMapVertexFunction}
            ${VertexLightBuffer}

            // Match doom behavior to not render flood view when camera is above/below ceiling/floor
            vec3 worldPos = mix(vertexPosFrag + cameraDirection * 0.001,
                vec3(0.0, 0.0, 0.0), 
                float(camera.z <= minViewZ || camera.z >= maxViewZ));

            gl_Position = mvp * vec4(worldPos, 1.0);
            depthFrag = gl_Position.${Depth};
        }
    "
    .Replace("${LightLevelVertexVariables}", LightLevel.VertexVariables(LightLevelOptions.NoDist))
    .Replace("${VertexLightBufferVariables}", LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", LightLevel.VertexLightBuffer(VertexLightBufferOptions.Default))
    .Replace("${SectorColorMapVertexFragVariables}", SectorColorMap.VertexFragVariables)
    .Replace("${SectorColorMapVertexUniformVariables}", SectorColorMap.VertexUniformVariables)
    .Replace("${SectorColorMapVertexFunction}", SectorColorMap.VertexFunctionWorld())
    .Replace("${VertexOptionsSet}", VertexFunction.VertexOptionsSet)
    .Replace("${ColorMapAndLightLevelSet}", VertexFunction.ColorMapAndLightLevelSet)
    .Replace("${Depth}", ShaderVars.Depth);

    protected override string FragmentShader()
    {
        if (this is FloodFillWallClipProgram)
            return PlaneClip.WriteWallFragFunction(WallClipFragOptions.DiscardNegativeMapId);

        return @"
            #version 330

            flat in float planeZFrag;
            flat in float mapIdFrag;
            flat in float upperFrag;
            flat in float lowerFrag;
            in vec3 vertexPosFrag;
            in float dist2D;

            out vec4 fragColor;

            uniform sampler2D boundTexture;
            uniform sampler2D brightmapTexture;
            uniform vec3 camera;
            uniform mat4 mvpNoPitch;
            uniform mat4 mvp;
            uniform int hasInvulnerability;
            uniform vec3 colorMix;
            uniform int paletteIndex;
            uniform int colormapIndex;
            uniform int useBrightmaps;
            uniform int useSectorFog;

            ${LightLevelFragVariables}
            ${SectorColorMapFragVariables}

            void main()
            {
                vec3 planeNormal = vec3(0, 0, 1);
                vec3 pointOnPlane = vec3(0, 0, planeZFrag);
                vec3 lookDir = normalize(vertexPosFrag - camera);
                float planeDot = dot(pointOnPlane - camera, planeNormal) / dot(lookDir, planeNormal);
                vec3 planePos = camera + (lookDir * planeDot);
                vec2 texDim = textureSize(boundTexture, 0);
                vec2 uvFrag = vec2(planePos.x / texDim.x, planePos.y / texDim.y);

                uvFrag.y = -uvFrag.y; // Vanilla textures are drawn top-down.

                float dist2D = (mvpNoPitch * vec4(planePos, 1.0)).${Depth};
                float dist3D = (mvp * vec4(planePos, 1.0)).${Depth};
                ${LightLevelFragFunction}
                ${SectorColorMapFragFunction}
                ${FragColorFunction}
            }
        "
        .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
        .Replace("${LightLevelFragVariables}", LightLevel.FragVariables(LightLevelOptions.NoDist))
        .Replace("${FragColorFunction}", FragFunction.FragColorFunction(FragColorFunctionOptions.Colormap | FragColorFunctionOptions.Brightmaps))
        .Replace("${Depth}", ShaderVars.Depth)
        .Replace("${SectorColorMapFragVariables}", SectorColorMap.FragVariables)
        .Replace("${SectorColorMapFragFunction}", SectorColorMap.FragFunction);
    }
}
