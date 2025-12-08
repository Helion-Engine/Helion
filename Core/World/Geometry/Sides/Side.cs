using Helion.Dehacked;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Maps.Specials;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;
using Helion.World.Static;
using System;

namespace Helion.World.Geometry.Sides;

public record class SideColormaps(Colormap? Upper, Colormap? Middle, Colormap? Lower);

public record struct FloodKeys(int Key1, int Key2);

public struct SideFlags
{
    public bool BlockmapLinked;
    public bool UpperSky;
    public bool LightLevelAbsolute;
    public bool NoFakeContrast;
    public bool SmoothLighting;
    public bool WrapMidTex;
}

public sealed class Side
{
    public static readonly FloodKeys NoFloodKeys = new(0, 0);

    public int Id;
    public Sector Sector;
    public Wall Upper;
    public Wall Middle;
    public Wall Lower;
    public Vec2I Offset;
    public Line Line;
    public SideDataTypes DataChanges;
    public bool DataChanged => DataChanges > 0;
    // This is currently just for the renderer to know for scrolling lines to not cache
    public bool OffsetChanged => ScrollData != null;
    public SectorDynamic Dynamic;
    public bool IsDynamic => Dynamic != SectorDynamic.None;
    public SideTexture FloodTextures;
    public byte LightLevel;

    public bool IsFront => this == Line.Front;
    public Side? PartnerSide => IsFront ? Line.Back : Line.Front;

    public SideScrollData? ScrollData;

    public SideColormaps? Colormaps;
    public StaticSideSkyData? SkyGeometry;

    public int LastRenderGametick;
    public int LastRenderGametickAlpha;
    public int BlockmapCount;
    public FloodKeys UpperFloodKeys;
    public FloodKeys LowerFloodKeys;
    public int FloorFloodKey;
    public int CeilingFloodKey;
    public SectorPlanes MidTextureFlood;
    public SideFlags Flags;

    public Side(int id, Vec2I offset, Wall upper, Wall middle, Wall lower, Sector sector)
        : this(id, offset, upper, middle, lower, sector, 0, false, false, false, false)
    {

    }

    public Side(int id, Vec2I offset, Wall upper, Wall middle, Wall lower, Sector sector, int lightLevel, 
        bool lightLevelAbsolute, bool noFakeContrast, bool smoothLighting, bool wrapMidTex)
    {
        Id = id;
        Sector = sector;
        Offset = offset;
        Upper = upper;
        Middle = middle;
        Lower = lower;

        // We are okay with things blowing up violently if someone forgets
        // to assign it, because that is such a critical error on the part
        // of the developer if this ever happens that it's deserved. Fixing
        // this would lead to some very messy logic, and when this is added
        // to a parent object, it will add itself for us. If this can be
        // fixed in the future with non-messy code, go for it.
        Line = null!;

        LightLevel = (byte)Math.Clamp(lightLevel, 0, 255);
        Flags.LightLevelAbsolute = lightLevelAbsolute;
        Flags.NoFakeContrast = noFakeContrast;
        Flags.SmoothLighting = smoothLighting;
        Flags.WrapMidTex = wrapMidTex;
    }

    public void Reset()
    {
        DataChanges = default;
        ScrollData = default;
        LastRenderGametick = default;
        LastRenderGametickAlpha = default;
        BlockmapCount = default;
        UpperFloodKeys = default;
        LowerFloodKeys = default;
        FloorFloodKey = default;
        CeilingFloodKey = default;
        MidTextureFlood = default;
        Flags.BlockmapLinked = default;
        Flags.UpperSky = default;

        Upper.Reset();
        Middle.Reset();
        Lower.Reset();
    }

    public void SetWallTexture(int texture, WallLocation location)
    {
        switch (location)
        {
            case WallLocation.Upper:
                Upper.TextureHandle = texture;
                DataChanges |= SideDataTypes.UpperTexture;
                break;
            case WallLocation.Lower:
                Lower.TextureHandle = texture;
                DataChanges |= SideDataTypes.LowerTexture;
                break;
            case WallLocation.Middle:
                Middle.TextureHandle = texture;
                DataChanges |= SideDataTypes.MiddleTexture;
                break;
        }
        Line.DataChanges |= LineDataTypes.Texture;
    }

    public override string ToString() => $"Id={Id} {GetSideToString()} | Line-{Line}";

    private string GetSideToString()
    {
        if (PartnerSide == null)
            return "1s";

        return $"2s-{(IsFront ? "f" : "b")}";
    }
}
