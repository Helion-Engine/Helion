using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Util.Configs.Components;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shared;

public struct RenderInfoOptions
{
    public bool SectorColor;
    public bool SectorFog;
    public bool SectorColorFetchChange;
}

/// <summary>
/// A simple container for render-related information.
/// </summary>
public class RenderInfo
{
    public static Vec2F LastAutomapOffset;

    public OldCamera Camera;
    public float TickFraction;
    public Rectangle Viewport;
    public Entity ViewerEntity;
    public bool DrawAutomap;
    public Vec2I AutomapOffset;
    public double AutomapScale;
    public ConfigRender Config;
    public ShaderUniforms Uniforms;
    public Sector ViewSector;
    public TransferHeightView TransferHeightView;
    public bool BrightMaps;
    public RenderInfoOptions Options;

    public RenderInfo()
    {
        // Set must be called on new allocation
        Camera = null!;
        Config = null!;
        ViewerEntity = null!;
        ViewSector = null!;

        Uniforms.GammaCorrection = 0f;
    }

    public void Set(OldCamera? camera, float tickFraction, Rectangle viewport, Entity viewerEntity, bool drawAutomap,
        Vec2I automapOffset, double automapScale, ConfigRender config, Sector viewSector, TransferHeightView transferHeightView,
        bool brightMaps, bool sectorColor, bool sectorFog, bool sectorColorFetchChange)
    {
        Precondition(tickFraction >= 0.0 && tickFraction <= 1.0, "Tick fraction should be in the unit range");

        if (camera != null)
            Camera = camera;
        TickFraction = tickFraction;
        Viewport = viewport;
        ViewerEntity = viewerEntity;
        DrawAutomap = drawAutomap;
        AutomapOffset = automapOffset;
        AutomapScale = automapScale;
        Config = config;
        ViewSector = viewSector;
        TransferHeightView = transferHeightView;
        BrightMaps = brightMaps;
        Options.SectorColor = sectorColor;
        Options.SectorFog = sectorFog;
        Options.SectorColorFetchChange = sectorColorFetchChange;

        if (!DrawAutomap)
            LastAutomapOffset = Vec2F.Zero;
    }
}
