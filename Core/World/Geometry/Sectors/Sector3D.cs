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
    LightBetween = 4,
    RenderInside = 8,
    VisibilityInvert = 16,
    ShootabilityInvert = 32,
    
    DisableLighting = 128,
    RestrictLighting = 256,
    Fog = 512,
    Model = 1024,
    UseUpperTexture = 2048,
    UserLowerTexture = 4096,
    AdditiveTransparency = 8192,
    Fade = 16384,
    ResetAbove = 32768
}

public class Sector3D
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
    public SectorFlags3D Flags;

    private readonly Entity Entity;

    public bool IsSolid => (Flags & SectorFlags3D.Solid) != 0;
    public bool IsSwimmable => (Flags & SectorFlags3D.Swim) != 0;
    public bool ShouldRenderWalls => ControlTop.Z - ControlBottom.Z > 0;
    public bool ShouldRenderInsideWalls => (Flags & SectorFlags3D.RenderInside) != 0;

    private static readonly Wall EmptyWall = new(Constants.NoTextureIndex, WallLocation.None);

    public Sector3D(IWorld world, int parentSectorId, Sector parentSector, Sector controlSector, int textureHandle, SectorFlags3D flags)
    {
        SectorId = world.Geometry.CreateNewSectorId();
        ParentSectorId = parentSectorId;
        FakeBottom = new(SectorPlaneFace.Floor, 0, 0, 0);
        FakeTop = new(SectorPlaneFace.Ceiling, 0, 0, 0);

        if ((flags & SectorFlags3D.Swim) != 0)
        {
            FakeTopFlipped = new(SectorPlaneFace.Ceiling, 0, 0, 0);
            FakeBottomFlipped = new(SectorPlaneFace.Floor, 0, 0, 0);
            FakeSectorFlipped = new(SectorId, 0, 0, FakeBottomFlipped, FakeTopFlipped, default, default);
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
        Entity = new();
        Entity.Set(-1, -1, 0, EntityDefinition.Default, default, 0, Sector.Default, world, default);
        Entity.Sector3D = this;

        if (IsSolid)
        {
            Entity.Flags.SetSolid();
            Entity.Flags.SetActLikeBridge();
        }
    }

    public void Reset()
    {
        for (int i = 0; i < FakeSector.Lines.Length; i++)
            FakeSector.Lines[i].Front.Reset();

        FakeBottom.Reset(0);
        FakeTop.Reset(0);
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

    public void CalculateWallHeights(out double topZ, out double bottomZ, out double prevTopZ, out double prevBottomZ)
    {
        topZ = ControlTop.Z;
        bottomZ = ControlBottom.Z;
        prevTopZ = ControlTop.PrevZ;
        prevBottomZ = ControlBottom.PrevZ;

        if (ParentSector.Sectors3D.Length == 0)
            return;

        var entity = GetSectorEntity3D();
        for (int i = 0; i < ParentSector.Sectors3D.Length; i++)
        {
            var checkSector3d = ParentSector.Sectors3D[i];
            if (checkSector3d == this)
                break;

            if (!entity.OverlapsZ(checkSector3d.GetSectorEntity3D()))
                continue;

            if (!AdjustWallHeights(ref topZ, ref bottomZ, ref prevTopZ, ref prevBottomZ, 
                checkSector3d.ControlTop.Z, checkSector3d.ControlBottom.Z, checkSector3d.ControlTop.PrevZ, checkSector3d.ControlBottom.PrevZ))
                break;
        }

        FakeTop.Z = topZ;
        FakeBottom.Z = bottomZ;
    }

    public static bool CalculateWallHeights(Side side, double topZ, double bottomZ, double prevTopZ, double prevBottomZ, 
        out double newTopZ, out double newBottomZ, out double newPrevTopZ, out double newPrevBottomZ)
    {
        newTopZ = topZ;
        newBottomZ = bottomZ;
        newPrevTopZ = prevTopZ;
        newPrevBottomZ = prevBottomZ;
        WallVertices wall = default;

        if (side.PartnerSide == null)
        {
            WorldTriangulator.HandleOneSided(side, side.Sector.Floor, side.Sector.Ceiling, default, ref wall);
            return AdjustWallHeights(ref newTopZ, ref newBottomZ, ref newPrevTopZ, ref newPrevBottomZ, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ);
        }

        if (side.PartnerSide != null)
        {
            if (GeometryRenderer.LowerIsVisible(side, side.Sector, side.PartnerSide.Sector))
            {
                WorldTriangulator.HandleTwoSidedLower(side, side.PartnerSide.Sector.Floor, side.Sector.Floor, default, true, ref wall);
                if (!AdjustWallHeights(ref newTopZ, ref newBottomZ, ref newPrevTopZ, ref newPrevBottomZ, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ))
                    return false;
            }

            if (GeometryRenderer.UpperIsVisible(side, side.Sector, side.PartnerSide.Sector))
            {
                WorldTriangulator.HandleTwoSidedUpper(side, side.PartnerSide.Sector.Floor, side.Sector.Floor, default, true, ref wall);
                if (!AdjustWallHeights(ref newTopZ, ref newBottomZ, ref newPrevTopZ, ref newPrevBottomZ, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ))
                    return false;
            }
        }

        return true;
    }

    private static bool AdjustWallHeights(ref double topZ, ref double bottomZ, ref double prevTopZ, ref double prevBottomZ, 
        double checkTopZ, double checkBottomZ, double checkPrevTopZ, double checkPrevBottomZ)
    {
        if (checkTopZ < topZ)
        {
            bottomZ = checkTopZ;
            prevBottomZ = checkPrevTopZ;
        }
        if (checkBottomZ > bottomZ)
        {
            topZ = checkBottomZ;
            prevTopZ = checkPrevBottomZ;
        }

        if (checkTopZ >= topZ && checkBottomZ <= bottomZ)
        {
            bottomZ = 0;
            topZ = 0;
            checkPrevTopZ = 0;
            checkPrevBottomZ = 0;
            return false;
        }

        return true;
    }

    public override string ToString() => $"3D Sector: {SectorId} ControlId: {ControlSector.Id} ParentId: {ParentSectorId} [{ControlSector.Ceiling.Z} {ControlSector.Floor.Z}]";
}
