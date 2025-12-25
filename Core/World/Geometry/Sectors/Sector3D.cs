using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shared.World;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using System;

namespace Helion.World.Geometry.Sectors;

[Flags]
public enum SectorFlags3D
{
    None = 0,
    Solid = 1,
    Swim = 2,
    RenderInside = 8,
    SightInvert = 16,
    ShootInvert = 32,
    
    DisableLighting = 128,
    RestrictLighting = 256,
    Fog = 512,
    Model = 1024,
    UseUpperTexture = 2048,
    UseLowerTexture = 4096,
    AdditiveTransparency = 8192,
    Fade = 65536,
    ResetAbove = 131072,

    // Helion Flags
    NoRender = 262144
}

public enum SectorLightFlags3D
{
    None,
    ToNextTypeZero, // Extra light extends from ceiling of control sector down to top of another type 0 light
    ToControlFloor, // Extra light extends from ceiling down to the floor of the control sector.
    ToNextAny // Extra light extends from control sector's ceiling down to the top of another extra light.
}

public struct WallHeights(double topZ, double bottomZ, double prevTopZ, double prevBottomZ)
{
    public double TopZ = topZ;
    public double BottomZ = bottomZ;
    public double PrevTopZ = prevTopZ;
    public double PrevBottomZ = prevBottomZ;
    public bool Invalid;
    public bool Clipped;
}

public enum SolidContext
{
    LineOfSight,
    HitScan
}

public sealed class Sector3D
{
    public int ParentSectorId;
    public int SectorId;
    public Sector ParentSector;
    public Sector ControlSector;
    public SectorPlane ControlTop;
    public SectorPlane ControlBottom;
    public Sector FakeSector;
    public SectorPlane FakeTop;
    public SectorPlane FakeBottom;
    public Sector? FakeSectorFlipped;
    public SectorPlane? FakeTopFlipped;
    public SectorPlane? FakeBottomFlipped;
    public Sector LightTop;
    public Sector LightBottom;
    public Sector? LightMiddle;
    public SectorFlags3D Flags;
    public SectorLightFlags3D LightFlags;
    public float Alpha;
    public double ClipBottomZ;
    public double ClipPrevBottomZ;

    private readonly Entity Entity;
    private WallHeights m_wallHeights;
    private int m_lastGameTick = -1;

    public bool IsSolid => (Flags & SectorFlags3D.Solid) != 0;
    public bool IsSwimmable => (Flags & SectorFlags3D.Swim) != 0;
    public bool ShouldRenderWalls => ControlTop.Z - ControlBottom.Z > 0 && (Flags & SectorFlags3D.NoRender) == 0;
    public bool ShouldRenderFlats => ControlTop.Z - ControlBottom.Z >= 0 && (Flags & SectorFlags3D.NoRender) == 0;
    public bool ShouldRenderInsideWalls => (Flags & SectorFlags3D.RenderInside) != 0;
    public RenderDataStyle RenderDataStyle;

    private static readonly Wall EmptyWall = new(Constants.NoTextureIndex, WallLocation.None);

    public Sector3D(IWorld world, int parentSectorId, Sector parentSector, Sector controlSector, int textureHandle, SectorFlags3D flags, SectorLightFlags3D lightFlags, float alpha)
    {
        SectorId = world.Geometry.CreateNewSectorId();
        ParentSectorId = parentSectorId;
        FakeBottom = new(SectorPlaneFace.Floor, 0, 0, 0);
        FakeTop = new(SectorPlaneFace.Ceiling, 0, 0, 0);

        if ((flags & (SectorFlags3D.Swim | SectorFlags3D.RenderInside)) != 0)
        {
            FakeTopFlipped = new(SectorPlaneFace.Ceiling, 0, 0, 0);
            FakeBottomFlipped = new(SectorPlaneFace.Floor, 0, 0, 0);
            FakeSectorFlipped = new(SectorId, 0, 0, FakeBottomFlipped, FakeTopFlipped, default, default)
            {
                Sector3D = this
            };
        }

        FakeSector = new(SectorId, 0, 0, FakeBottom, FakeTop, default, default)
        {
            Sector3D = this,
            Lines = CreateSector3DLines(world, world.Sectors[parentSectorId], textureHandle, (flags & SectorFlags3D.RenderInside) != 0)
        };

        ParentSector = parentSector;
        ControlSector = controlSector;
        ControlTop = controlSector.Ceiling;
        ControlBottom = controlSector.Floor;
        LightTop = ParentSector;
        LightBottom = ParentSector;
        Flags = flags;
        LightFlags = lightFlags;
        Alpha = alpha;

        if ((Flags & SectorFlags3D.AdditiveTransparency) != 0)
            RenderDataStyle = RenderDataStyle.Add;
        else if (Alpha < 1)
            RenderDataStyle = RenderDataStyle.Translucent;
        else
            RenderDataStyle = RenderDataStyle.Normal;

        Entity = new();
        Entity.Set(-1, -1, 0, EntityDefinition.Default, default, 0, Sector.Default, world, default);
        Entity.Sector3D = this;

        if (IsSolid)
        {
            Entity.Flags.SetSolid();
            Entity.Flags.SetActLikeBridge();
        }

        ClipBottomZ = ControlBottom.Z;
        ClipPrevBottomZ = ControlBottom.PrevZ;
    }

    public void Reset()
    {
        for (int i = 0; i < FakeSector.Lines.Length; i++)
            FakeSector.Lines[i].Front.Reset();

        FakeBottom.Reset(0);
        FakeTop.Reset(0);
    }

    public bool IsInvertedByContext(SolidContext context)
    {
        if (context == SolidContext.LineOfSight)
            return (Flags & SectorFlags3D.SightInvert) != 0;
        else
            return (Flags & SectorFlags3D.ShootInvert) != 0;
    }

    public bool IsSolidByContext(SolidContext context)
    {
        var isSolid = IsSolid;
        if ((context == SolidContext.LineOfSight && (Flags & SectorFlags3D.SightInvert) != 0) ||
            (context == SolidContext.HitScan && (Flags & SectorFlags3D.ShootInvert) != 0))
        {
            return !isSolid;
        }

        return isSolid;
    }

    private static Line[] CreateSector3DLines(IWorld world, Sector sector, int textureHandle, bool createBackSide)
    {
        var lines = new Line[sector.Lines.Length];
        for (int i = 0; i < sector.Lines.Length; i++)
        {
            var line = sector.Lines[i];
            var middle = new Wall(textureHandle, WallLocation.Middle3D);
            var side = new Side(world.Geometry.CreateNewSideId(), line.Front.Offset, EmptyWall, middle, EmptyWall, sector);
            // Normalize so front is always the rendered side
            var lineSeg = line.Segment;
            if (line.Front.Sector == sector)
                lineSeg = new(lineSeg.End, lineSeg.Start);

            var backSide = createBackSide ? new Side(world.Geometry.CreateNewSideId(), line.Front.Offset, EmptyWall, new(textureHandle, WallLocation.Middle3D), EmptyWall, sector) : null;
            lines[i] = new Line(line.Id, lineSeg, side, backSide, default, LineSpecial.Default, default);
        }
        return lines;
    }

    public Entity GetSectorEntity3D()
    {
        Entity.PrevPosition.Z = ControlBottom.PrevZ;
        Entity.Position.Z = ControlBottom.Z;
        Entity.Height = Math.Max(ControlTop.Z - ControlBottom.Z, 0);
        return Entity;
    }

    public SectorPlane GetOpposingPlane3D(SectorPlaneFace face, double height = double.MinValue)
    {
        if (face == SectorPlaneFace.Floor)
        {
            var sector = GetNextHighestCeiling3D(height);
            if (sector == ParentSector)
                return sector.Ceiling;
            return sector.Floor;
        }
        else
        {
            var sector = GetNextLowestFloor3D(height);
            if (sector == ParentSector)
                return sector.Floor;
            return sector.Ceiling;
        }
    }

    public Sector GetNextHighestCeiling3D(double height = double.MinValue, SectorFlags3D skipFlags = SectorFlags3D.None)
    {
        var minFloorAbove = double.MaxValue;
        if (height == double.MinValue)
            height = ControlTop.Z;
        Sector3D? minSector = null;
        for (int i = 0; i < ParentSector.Sectors3D.Length; i++)
        {
            var sector = ParentSector.Sectors3D[i];
            if ((sector.Flags & skipFlags) != 0)
                continue;

            if (sector.ControlBottom.Z <= minFloorAbove && sector.ControlBottom.Z >= height)
            {
                minFloorAbove = sector.ControlBottom.Z;
                minSector = sector;
            }
        }

        return minSector?.ControlSector ?? ParentSector;
    }

    public Sector GetNextLowestFloor3D(double height = double.MinValue)
    {
        var maxCeilingBelow = double.MinValue;
        if (height == double.MinValue)
            height = ControlBottom.Z;
        Sector3D? maxSector = null;
        for (int i = 0; i < ParentSector.Sectors3D.Length; i++)
        {
            var sector = ParentSector.Sectors3D[i];
            if (sector.ControlTop.Z >= maxCeilingBelow && sector.ControlTop.Z <= height)
            {
                maxCeilingBelow = sector.ControlTop.Z;
                maxSector = sector;
            }
        }

        return maxSector?.ControlSector ?? ParentSector;
    }

    public void InvalidateWallHeights()
    {
        m_lastGameTick = -1;
    }

    public WallHeights CalculateWallHeights(int gameTick)
    {
        if (gameTick == m_lastGameTick)
            return m_wallHeights;

        m_lastGameTick = gameTick;
        m_wallHeights = new WallHeights(ControlTop.Z, ControlBottom.Z, ControlTop.PrevZ, ControlBottom.PrevZ);
        ClipBottomZ = ControlBottom.Z;

        if (ParentSector.Sectors3D.Length == 0)
            return m_wallHeights;

        for (int i = 0; i < ParentSector.Sectors3D.Length; i++)
        {
            var checkSector3D = ParentSector.Sectors3D[i];
            if (checkSector3D == this)
            {
                if (i < ParentSector.Sectors3D.Length - 1)
                {
                    checkSector3D = ParentSector.Sectors3D[i + 1];
                    if (checkSector3D.ControlTop.Z > m_wallHeights.BottomZ && ShouldClipSector3D(checkSector3D))
                    {
                        m_wallHeights.Clipped = true;
                        m_wallHeights.TopZ = checkSector3D.ControlTop.Z;
                        m_wallHeights.PrevTopZ = checkSector3D.ControlTop.PrevZ;
                        ClipBottomZ = checkSector3D.ControlTop.Z;
                    }
                }

                break;
            }

            if (ControlBottom.Z < checkSector3D.ControlTop.Z && ControlTop.Z > checkSector3D.ClipBottomZ)
            {
                if (!AdjustWallHeights(ref m_wallHeights,
                    checkSector3D.ControlTop.Z, checkSector3D.ClipBottomZ, checkSector3D.ControlTop.PrevZ, checkSector3D.ClipPrevBottomZ))
                    break;
            }
        }

        FakeTop.Z = m_wallHeights.TopZ;
        FakeBottom.Z = m_wallHeights.BottomZ;
        return m_wallHeights;
    }

    public bool CalculateWallHeights(Side side, in WallHeights wallHeights, out WallHeights newWallHeights)
    {
        newWallHeights = wallHeights;
        WallVertices wall = default;

        if (side.PartnerSide == null)
        {
            WorldTriangulator.HandleOneSided(side, side, side.Sector.Floor, side.Sector.Ceiling, default, ref wall, calculateUV: false);
            return AdjustWallHeights(ref newWallHeights, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ);
        }

        if (side.PartnerSide != null)
        {
            if (GeometryRenderer.LowerIsVisible(side, side.Sector, side.PartnerSide.Sector))
            {
                WorldTriangulator.HandleTwoSidedLower(side, side.PartnerSide.Sector.Floor, side.Sector.Floor, default, true, ref wall, calculateUV: false);
                if (WallVerticesOccluded(newWallHeights, wall))
                    return false;

                if (!AdjustWallHeights(ref newWallHeights, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ))
                    return false;
            }

            if (GeometryRenderer.UpperIsVisible(side, side.Sector, side.PartnerSide.Sector))
            {
                WorldTriangulator.HandleTwoSidedUpper(side, side.Sector.Ceiling, side.PartnerSide.Sector.Ceiling, default, true, ref wall, calculateUV: false);
                if (WallVerticesOccluded(newWallHeights, wall))
                    return false;

                if (!AdjustWallHeights(ref newWallHeights, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ))
                    return false;
            }
        }

        // Only clip if this side has 3D sectors. Otherwise overlapping non-solid sectors won't render.
        if (side.PartnerSide == null || side.PartnerSide.Sector.Sectors3D.Length > 0)
        {
            if (!AdjustWallHeights3D(side.Sector, ref newWallHeights))
                return false;
        }

        if (side.PartnerSide != null && side.Sector.Sectors3D.Length > 0)
        {
            if (!AdjustWallHeights3D(side.PartnerSide.Sector, ref newWallHeights))
                return false;
        }

        return true;
    }

    private static bool WallVerticesOccluded(in WallHeights newWallHeights, in WallVertices wall)
    {
        return wall.TopLeft.Z >= newWallHeights.TopZ && wall.BottomRight.Z <= newWallHeights.BottomZ;
    }

    private bool AdjustWallHeights3D(Sector sector, ref WallHeights newWallHeights)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3d = sector.Sectors3D[i];
            if (!ShouldClipSector3D(sector3d))
                continue;

            if (!AdjustWallHeights(ref newWallHeights, sector3d.ControlTop.Z, sector3d.ControlBottom.PrevZ, sector3d.ControlTop.Z, sector3d.ControlBottom.PrevZ))
                return false;
        }

        return true;
    }

    private bool ShouldClipSector3D(Sector3D other)
    {
        if (other == this)
            return false;

        var currentSolid = Flags & SectorFlags3D.Solid;
        var otherSolid = other.Flags & SectorFlags3D.Solid;

        if (RenderDataStyle != RenderDataStyle.Normal && other.RenderDataStyle != RenderDataStyle.Normal)
            return true;

        if (currentSolid != 0 && otherSolid != 0)
            return false;

        return currentSolid == otherSolid;
    }

    private static bool AdjustWallHeights(ref WallHeights wallHeights,
        double checkTopZ, double checkBottomZ, double checkPrevTopZ, double checkPrevBottomZ)
    {
        if (checkTopZ < wallHeights.TopZ && checkTopZ >= wallHeights.BottomZ)
        {
            wallHeights.BottomZ = checkTopZ;
            wallHeights.PrevBottomZ = checkPrevTopZ;
        }

        if (checkBottomZ > wallHeights.BottomZ && checkBottomZ <= wallHeights.TopZ)
        {
            wallHeights.TopZ = checkBottomZ;
            wallHeights.PrevTopZ = checkPrevBottomZ;
        }

        if (checkTopZ >= wallHeights.TopZ && checkBottomZ <= wallHeights.BottomZ)
        {
            wallHeights.Invalid = true;
            wallHeights.BottomZ = 0;
            wallHeights.TopZ = 0;
            wallHeights.PrevTopZ = 0;
            wallHeights.PrevBottomZ = 0;
            return false;
        }

        return true;
    }

    public int GetTextureHandle(Side controlSectorSide, Side? parentSectorSide)
    {
        if (parentSectorSide != null)
        {
            if ((Flags & SectorFlags3D.UseUpperTexture) != 0)
                return parentSectorSide.Upper.TextureHandle;
            if ((Flags & SectorFlags3D.UseLowerTexture) != 0)
                return parentSectorSide.Lower.TextureHandle;
        }
        return controlSectorSide.Middle.TextureHandle;
    }

    public override string ToString() => $"3D Sector={SectorId} ControlId={ControlSector.Id} ParentId={ParentSectorId} [{ControlSector.Floor.Z} -> {ControlSector.Ceiling.Z}]";
}
