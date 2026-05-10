using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;
using Helion.Util.Configs.Components;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public abstract class RenderProgramBase : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_sectorLightTextureLocation;
    private readonly int m_colormapTextureLocation;
    private readonly int m_sectorColormapTextureLocation;
    private readonly int m_sectorFogTextureLocation;
    private readonly int m_brightmapTextureLocation;
    private readonly int m_mvpLocation;
    private readonly int m_timeFracLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_mvpNoPitchLocation;
    private readonly int m_lightLevelMixLocation;
    private readonly int m_extraLightLocation;
    private readonly int m_distanceOffsetLocation;
    private readonly int m_colorMixLocation;
    private readonly int m_paletteIndexLocation;
    private readonly int m_colorMapIndexLocation;
    private readonly int m_lightModeLocation;
    private readonly int m_gammaCorrectionLocation;
    private readonly int m_vertexGapClampUV;
    private readonly int m_useBrightmapsLocation;
    private readonly int m_accumTextureLocation;
    private readonly int m_accumCountTextureLocation;
    private readonly int m_planeClipTextureLocation;
    private readonly int m_checkPlaneClipLocation;
    private readonly int m_wallClipTextureLocation;
    private readonly int m_downScaleAmountLocation;
    private readonly int m_screenBoundsLocation;
    private readonly int m_fogBarrierLocation;
    private readonly int m_useSectorColorLocation;
    private readonly int m_useSectorFogLocation;

    public RenderProgramBase(string label) : base(label)
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_sectorLightTextureLocation = Uniforms.GetLocation("sectorLightTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_sectorColormapTextureLocation = Uniforms.GetLocation("sectorColormapTexture");
        m_sectorFogTextureLocation = Uniforms.GetLocation("sectorFogTexture");
        m_brightmapTextureLocation = Uniforms.GetLocation("brightmapTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_timeFracLocation = Uniforms.GetLocation("timeFrac");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_mvpNoPitchLocation = Uniforms.GetLocation("mvpNoPitch");
        m_lightLevelMixLocation = Uniforms.GetLocation("lightLevelMix");
        m_extraLightLocation = Uniforms.GetLocation("extraLight");
        m_distanceOffsetLocation = Uniforms.GetLocation("distanceOffset");
        m_colorMixLocation = Uniforms.GetLocation("colorMix");
        m_paletteIndexLocation = Uniforms.GetLocation("paletteIndex");
        m_colorMapIndexLocation = Uniforms.GetLocation("colormapIndex");
        m_lightModeLocation = Uniforms.GetLocation("lightMode");
        m_gammaCorrectionLocation = Uniforms.GetLocation("gammaCorrection");
        m_vertexGapClampUV = Uniforms.GetLocation("vertexGapClampUV");
        m_useBrightmapsLocation = Uniforms.GetLocation("useBrightmaps");
        m_accumTextureLocation = Uniforms.GetLocation("accum");
        m_accumCountTextureLocation = Uniforms.GetLocation("accumCount");
        m_planeClipTextureLocation = Uniforms.GetLocation("planeClipTexture");
        m_checkPlaneClipLocation = Uniforms.GetLocation("checkPlaneClip");
        m_wallClipTextureLocation = Uniforms.GetLocation("wallClipTexture");
        m_downScaleAmountLocation = Uniforms.GetLocation("downScaleAmount");
        m_screenBoundsLocation = Uniforms.GetLocation("screenBounds");
        m_fogBarrierLocation = Uniforms.GetLocation("fogBarrier");
        m_useSectorColorLocation = Uniforms.GetLocation("useSectorColor");
        m_useSectorFogLocation = Uniforms.GetLocation("useSectorFog");
    }

    public void BoundTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_boundTextureLocation);
    public void SectorLightTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorLightTextureLocation);
    public void ColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_colormapTextureLocation);
    public void SectorColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorColormapTextureLocation);
    public void SectorFogTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorFogTextureLocation);
    public void BrightmapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_brightmapTextureLocation);
    public void AccumTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_accumTextureLocation);
    public void AccumCountTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_accumCountTextureLocation);
    public void PlaneClipTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_planeClipTextureLocation);
    public void WallClipTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_wallClipTextureLocation);

    public void HasInvulnerability(bool invul) => ProgramUniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mvp) => ProgramUniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => ProgramUniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void TimeFrac(float frac) => ProgramUniforms.Set(frac, m_timeFracLocation);
    public void LightLevelMix(float lightLevelMix) => ProgramUniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => ProgramUniforms.Set(extraLight, m_extraLightLocation);
    public void DistanceOffset(float distance) => ProgramUniforms.Set(distance, m_distanceOffsetLocation);
    public void ColorMix(Vec3F color) => ProgramUniforms.Set(color, m_colorMixLocation);
    public void PaletteIndex(int index) => ProgramUniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => ProgramUniforms.Set(index, m_colorMapIndexLocation);
    public void LightMode(RenderLightMode mode) => ProgramUniforms.Set((int)mode, m_lightModeLocation);
    public void GammaCorrection(float value) => ProgramUniforms.Set(value, m_gammaCorrectionLocation);
    public void VertexGapClampUV(bool value) => ProgramUniforms.Set(value, m_vertexGapClampUV);
    public void UseBrightmaps(bool value) => ProgramUniforms.Set(value, m_useBrightmapsLocation);
    public void CheckPlaneClip(bool value) => ProgramUniforms.Set(value, m_checkPlaneClipLocation);
    public void SetSpriteClipDownScaleAmount(float value) => ProgramUniforms.Set(value, m_downScaleAmountLocation);
    public void ScreenBounds(Vec2I value) => ProgramUniforms.Set(value, m_screenBoundsLocation);
    public void FogBarrier(bool value) => ProgramUniforms.Set(value, m_fogBarrierLocation);
    public void UseSectorColor(bool value) => ProgramUniforms.Set(value, m_useSectorColorLocation);
    public void UseSectorFog(int value) => ProgramUniforms.Set(value, m_useSectorFogLocation);
}
