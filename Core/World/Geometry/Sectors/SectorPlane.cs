using Helion.Geometry.Planes;
using Helion.Maps.Specials;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.World.Static;

namespace Helion.World.Geometry.Sectors;

public sealed class SectorPlane : SectorSoundSource
{
    private static int StaticPlaneId;

    public SectorPlaneFace Facing;
    public PlaneD Plane;
    public Sector Sector;
    public double Z;
    public double PrevZ;
    public int TextureHandle;
    public short LightLevel;
    public short LightLevelAdd;
    public int LastRenderChangeGametick;
    public int LastRenderGametick;
    public int Id;

    public RenderOffsets RenderOffsets;
    public SectorDynamic Dynamic;
    public StaticGeometryData Static;

    public bool MidTextureHack;
    public bool NoRender;
    public bool LightLevelAbsolute;
    public StaticSkyGeometryData? SkyGeometry;
    public FlatTransformMethod FlatTransformMethod;
    public override Sector SoundSector => Sector;

    private readonly double m_initialZ;
    private readonly int m_initialTextureHandle;
    private readonly RenderOffsets m_initialRenderOffsets;

    public SectorPlane(SectorPlaneFace facing, double z, int textureHandle, short lightLevel, in RenderOffsets renderOffsets = default)
    {
        Id = ++StaticPlaneId;
        Facing = facing;
        Z = z;
        PrevZ = z;
        TextureHandle = textureHandle;
        LightLevel = lightLevel;
        Plane = new PlaneD(0, 0, 1.0, -z);
        m_initialZ = z;
        m_initialTextureHandle = textureHandle;
        Sector = null!;
        RenderOffsets = renderOffsets;
        m_initialRenderOffsets = renderOffsets;
    }

    public static void ResetId()
    {
        StaticPlaneId = 0;
    }

    public void Reset(short lightLevel)
    {
        ResetSound();
        SetZ(m_initialZ);
        PrevZ = m_initialZ;
        TextureHandle = m_initialTextureHandle;
        LightLevel = lightLevel;
        LastRenderChangeGametick = default;
        LastRenderGametick = default;
        Dynamic = default;
        Static = default;
        MidTextureHack = default;
        NoRender = default;
        SkyGeometry = default;

        RenderOffsets = m_initialRenderOffsets;
    }

    public void SetZ(double z)
    {
        Plane.MoveZ(z - Z);
        Z = z;
    }

    public void SetSectorMoveChanged(int gametick) => LastRenderChangeGametick = gametick;

    public bool CheckRenderingChanged()
    {
        if (LastRenderChangeGametick >= LastRenderGametick - 1)
            return true;

        if (PrevZ != Z)
            return true;

        if (RenderOffsets.Gametick != 0)
            return true;

        return false;
    }

    public void SetTexture(int texture, int gametick)
    {
        TextureHandle = texture;
        Sector.PlaneTextureChange(this);
        LastRenderChangeGametick = gametick;
    }

   

    public override string ToString() => $"Id={Id} Z={Z} Face={Facing} Texture={TextureHandle}";
}
