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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    // Also forces this sector to the reset light sector.
    RestrictLighting = 256,
    Fog = 512,
    CeilingModel = 1024,
    UseParentUpperTexture = 2048,
    UseParentLowerTexture = 4096,
    AdditiveTransparency = 8192,
    // When this flag is not set UZDoom will render the fog color as a global blend color and not have fade density. When set it renders the fog color in the 3D sector as expected.
    NoViewFade = 65536,
    // Resets the light sector to the parent sector. Will cause any sector with RestrictLighting to use this reset sector for light properties.
    ResetLight = 131072,

    // Helion Flags
    NoRender = 1 << 18,
    LightTransfer = 1 << 19
}

public enum SectorLightFlags3D
{
    None,
    ToNextTypeZero, // Extra light extends from ceiling of control sector down to top of another type 0 light
    ToControlFloor, // Extra light extends from ceiling down to the floor of the control sector.
    ToNextAny // Extra light extends from control sector's ceiling down to the top of another extra light.
}

public enum ClipStyle
{
    Solid,
    NotSolid
}

public enum SetHeightsMode
{
    Init,
    Update,
    MapReload
}

public struct WallHeights(double topZ, double bottomZ, double prevTopZ, double prevBottomZ)
{
    public double TopZ = topZ;
    public double BottomZ = bottomZ;
    public double PrevTopZ = prevTopZ;
    public double PrevBottomZ = prevBottomZ;
    public bool Invalid;
    public bool Clipped;
    public bool SetClipped;

    public readonly override string ToString() => $"[{BottomZ} -> {TopZ}] Invalid={Invalid} Clipped={Clipped}";
}

public enum SolidContext
{
    LineOfSight,
    HitScan
}

public sealed class Sector3D
{
    public const double InvalidZ = short.MinValue;

    public int ParentSectorId;
    public int SectorId;
    public int CheckCount;
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
    public SectorLightFlags3D LightFlags;
    public float Alpha;
    public double ClipBottomZ;
    public double ClipPrevBottomZ;
    public WallHeights WallHeights;
    public WallHeights WallHeightsUnclipped;
    public SectorPlanes RenderPlanes;
    public double LastSlopeTop;
    public double LastSlopeBottom;
    public int LastSlopeCheckCount;
    public Side ControlSide;

    private readonly Entity Entity;

    public bool IsSolid => (Flags & SectorFlags3D.Solid) != 0;
    public bool IsSwimmable => (Flags & SectorFlags3D.Swim) != 0;
    public bool IsLightTransfer => (Flags & SectorFlags3D.LightTransfer) != 0;
    public bool ShouldRenderWalls => ControlTop.Z - ControlBottom.Z > 0 && (Flags & SectorFlags3D.NoRender) == 0;
    public bool ShouldRenderFlats => ControlTop.Z - ControlBottom.Z >= 0 && (Flags & SectorFlags3D.NoRender) == 0;
    public bool ShouldRenderInsideWalls => (Flags & SectorFlags3D.RenderInside) != 0;
    public bool IsOpaque => RenderDataStyle == RenderDataStyle.Normal;
    public RenderDataStyle RenderDataStyle;
    public ClipStyle ClipStyle;

    private static readonly Wall EmptyWall = new(Constants.NoTextureIndex, WallLocation.None);
    private static readonly Line NoRenderLine3D = new(0, default, new(0, default, EmptyWall, EmptyWall, EmptyWall, Sector.Default), null, default, LineSpecial.Default, default)
    {
        NoRenderSector3D = true
    };
    private static readonly Comparison<Sector3D> SortSectors3D = new(HeightCompare);
    public static readonly Comparison<SectorPlane3D> SortPlanesByKey3D = new(PlaneHeightKeyCompare);

    public Sector3D(IWorld world, int parentSectorId, Sector parentSector, Sector controlSector, Side controlSide, SectorFlags3D flags, SectorLightFlags3D lightFlags, float alpha)
    {
        ParentSector = parentSector;
        ControlSector = controlSector;
        ControlTop = controlSector.Ceiling;
        ControlBottom = (flags & SectorFlags3D.CeilingModel) == 0 ? controlSector.Floor : controlSector.Ceiling;
        LightTop = ParentSector;
        LightBottom = ParentSector;
        Flags = flags;
        LightFlags = lightFlags;
        Alpha = alpha;
        ControlSide = controlSide;

        SectorId = world.Geometry.CreateNewSectorId();
        ParentSectorId = parentSectorId;
        FakeTop = new(SectorPlaneFace.Ceiling, ControlTop.Z, 0, 0);
        FakeBottom = new(SectorPlaneFace.Floor, ControlBottom.Z, 0, 0);
        FakeTop.RenderOffsets = controlSector.Floor.RenderOffsets;
        FakeBottom.RenderOffsets = controlSector.Ceiling.RenderOffsets;

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
            Lines = CreateSector3DLines(world, world.Sectors[parentSectorId], controlSide, (flags & SectorFlags3D.RenderInside) != 0)
        };

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

        ClipStyle = RenderDataStyle == RenderDataStyle.Normal && (Flags & SectorFlags3D.LightTransfer) == 0 ? ClipStyle.Solid : ClipStyle.NotSolid;

        // Currently can't render this because of the shader fetch based on sector id. Likely doesn't matter since it's an unviewable control sector anyway.
        if ((Flags & SectorFlags3D.NoViewFade) == 0)
            ControlSector.IgnoreFogColor = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetOffsetX() => ControlSide.Middle.Offset.X + ControlSide.Offset.X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetOffsetY() => ControlSide.Middle.Offset.Y + ControlSide.Offset.Y;

    public static void SetHeights3D(Sector sector, SetHeightsMode mode)
    {
        if (sector.Sectors3D.Length == 0)
            return;

        if (mode == SetHeightsMode.Update)
            sector.HeightsUpdated3D = true;

        SetupPlanesAndSort(sector);

        var currentLightSector = sector;
        Sector3D? currentLightSector3D = null;
        Sector resetLightSector = sector;
        Sector3D? resetLightSector3D = null;
        var resetLight = false;
        var resetZ = double.MaxValue;
        ref var lastPlane3D = ref sector.SectorPlanes3D[0];
        ref var overlapLightPlane3D = ref sector.SectorPlanes3D[0];

        for (int i = 0; i < sector.SectorPlanes3D.Length; i++)
        {
            ref var plane3D = ref sector.SectorPlanes3D[i];
            plane3D.NoRenderWall = false;
            var sector3D = plane3D.Sector3D;
            
            // If this plane is the same height as the last for transfer light then don't reset here.
            if (resetLight && resetZ != plane3D.GetZ())
            {
                resetZ = double.MaxValue;
                resetLight = false;
                currentLightSector3D = resetLightSector3D;
                currentLightSector = resetLightSector;
            }

            if (sector3D == null)
            {
                plane3D.LightSector = currentLightSector;

                if (plane3D.Face == PlaneFace3D.Bottom)
                    plane3D.Plane.Sector.TransferFloorLightSector = currentLightSector;
                else
                    plane3D.Plane.Sector.TransferCeilingLightSector = currentLightSector;

                if (i > 0 && lastPlane3D.Sector3D != null && lastPlane3D.ControlPlane.Z == plane3D.Plane.Z && lastPlane3D.Face == plane3D.Face)
                {
                    var keepPlane = (SectorPlanes)(lastPlane3D.Face + 1);
                    lastPlane3D.Sector3D.RenderPlanes &= keepPlane;
                }

                continue;
            }

            var overlapLight = false;
            if (i > 0 && lastPlane3D.Sector3D != null && lastPlane3D.ControlPlane.Z == plane3D.Plane.Z)
            {
                if (lastPlane3D.Face == plane3D.Face && !sector3D.ShouldRenderInsideWalls)
                {
                    // Flag previous plane not to render since this one takes precedence
                    var keepPlane = (SectorPlanes)(lastPlane3D.Face + 1);
                    lastPlane3D.Sector3D.RenderPlanes &= keepPlane;
                    lastPlane3D.NoRenderWall = true;
                }
                else if ((lastPlane3D.Sector3D.Flags & SectorFlags3D.NoRender) == 0 && lastPlane3D.Sector3D != plane3D.Sector3D && lastPlane3D.Sector3D.RenderDataStyle == RenderDataStyle.Normal)
                {
                    // Flag to update lighting for the previous 3D sector
                    overlapLight = true;
                    overlapLightPlane3D = ref lastPlane3D;
                }
            }

            // If solid bottom plane was clipped then it's not visible
            // TODO should be able to determine for solid top plane
            if (i > 0 && lastPlane3D.Sector3D != null && lastPlane3D.Face == PlaneFace3D.Bottom &&
                lastPlane3D.Sector3D.IsSolid && lastPlane3D.Sector3D.ClipBottomZ != lastPlane3D.ControlPlane.Z)
                lastPlane3D.Sector3D.RenderPlanes &= ~SectorPlanes.Floor;

            lastPlane3D = ref plane3D;
            SetLight(sector3D, ref plane3D, currentLightSector);

            if (resetLight)
            {
                resetZ = double.MaxValue;
                resetLight = false;
                currentLightSector3D = resetLightSector3D;
                currentLightSector = resetLightSector;
            }

            if ((sector3D.Flags & SectorFlags3D.ResetLight) != 0)
            {
                resetLightSector = sector;
                resetLightSector3D = null;
            }
            else if (ShouldResetLightSector(plane3D, sector3D))
            {
                resetLightSector = sector3D.ControlSector;
                resetLightSector3D = sector3D;
            }

            if (ShouldCarryLight(currentLightSector3D, sector3D, plane3D, true, out resetLight, out resetZ))
                continue;

            currentLightSector = sector3D.ControlSector;
            currentLightSector3D = sector3D;

            if (overlapLight && overlapLightPlane3D.Sector3D != null)
                SetLight(overlapLightPlane3D.Sector3D, ref overlapLightPlane3D, currentLightSector);
        }
    }

    private static void SetupPlanesAndSort(Sector sector)
    {
        if (sector.SectorPlanes3D.Length == 0)
        {
            sector.SectorPlanes3D = new SectorPlane3D[(sector.Sectors3D.Length + 1) * 2];
            var index = 0;

            for (int i = 0; i < sector.Sectors3D.Length; i++)
            {
                var sector3D = sector.Sectors3D[i];
                sector.SectorPlanes3D[index++] = new(sector3D.ControlTop, sector3D.FakeTop, sector3D, PlaneFace3D.Top, sector3D.ControlSector);
                sector.SectorPlanes3D[index++] = new(sector3D.ControlBottom, sector3D.FakeBottom, sector3D, PlaneFace3D.Bottom, sector3D.ControlSector);
            }

            sector.SectorPlanes3D[index++] = new(sector.Ceiling, sector.Ceiling, null, PlaneFace3D.Top, sector);
            sector.SectorPlanes3D[index++] = new(sector.Floor, sector.Floor, null, PlaneFace3D.Bottom, sector);
        }

        sector.TransferHeights = null;
        sector.Sectors3D.Sort(SortSectors3D);

        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3D = sector.Sectors3D[i];
            sector3D.CalculateHeights();
            sector3D.RenderPlanes = SectorPlanes.Floor | SectorPlanes.Ceiling;
        }

        for (int i = 0; i < sector.SectorPlanes3D.Length; i++)
            sector.SectorPlanes3D[i].UpdateSortKey();

        sector.SectorPlanes3D.Sort(SortPlanesByKey3D);
    }

    private static bool ShouldResetLightSector(in SectorPlane3D plane3D, Sector3D sector3D)
    {
        if (!sector3D.IsLightTransfer)
            return (sector3D.Flags & SectorFlags3D.RestrictLighting) == 0;

        var planeZ = plane3D.GetZ();
        return planeZ < sector3D.ControlTop.Z && planeZ > sector3D.ControlBottom.Z;
    }

    private static int PlaneHeightKeyCompare(SectorPlane3D x, SectorPlane3D y)
    {
        return x.SortKey.CompareTo(y.SortKey);
    }

    private static void SetLight(Sector3D sector3D, ref SectorPlane3D plane3D, Sector lightSector)
    {
        plane3D.LightSector = lightSector;

        if ((sector3D.Flags & SectorFlags3D.RestrictLighting) == 0)
            plane3D.LightInsideSector = lightSector;
        else
            plane3D.LightInsideSector = sector3D.ControlSector;

        if (plane3D.Face == PlaneFace3D.Bottom)
            sector3D.LightBottom = lightSector;
        else
            sector3D.LightTop = lightSector;
    }

    private static bool ShouldCarryLight(Sector3D? currentLightSector3D, Sector3D nextSector3D, in SectorPlane3D nextPlane3D, bool checkLightTransfer, out bool resetLight, out double resetZ)
    {
        resetZ = double.MaxValue;
        resetLight = false;

        if ((nextSector3D.Flags & SectorFlags3D.RestrictLighting) != 0)
        {
            resetLight = true;
            return true;
        }

        if ((nextSector3D.Flags & SectorFlags3D.DisableLighting) != 0)
            return true;

        if (currentLightSector3D == null)
            return false;

        if (checkLightTransfer && currentLightSector3D.LightFlags != SectorLightFlags3D.None)
        {
            switch(currentLightSector3D.LightFlags)
            {
                // These don't appear to work how they are documented. Both to next types need to allow another to render.
                case SectorLightFlags3D.ToNextTypeZero:
                case SectorLightFlags3D.ToNextAny:
                    if (nextSector3D != currentLightSector3D && nextSector3D.LightFlags != SectorLightFlags3D.None)
                        return false;
                    break;
                case SectorLightFlags3D.ToControlFloor:
                    if (nextSector3D == currentLightSector3D && nextPlane3D.Face == PlaneFace3D.Bottom)
                    {
                        resetZ = nextPlane3D.GetZ();
                        resetLight = true;
                    }
                    break;
            }

            return true;
        }

        var nextFlags = nextSector3D.Flags;
        var currentFlags = currentLightSector3D.Flags;

        if ((currentFlags & SectorFlags3D.Swim) != 0)
            return (nextFlags & SectorFlags3D.Swim) == 0;

        if ((currentFlags & SectorFlags3D.Swim) != (nextFlags & SectorFlags3D.Swim))
            return false;

        if ((currentFlags & SectorFlags3D.LightTransfer) != 0 || (nextFlags & SectorFlags3D.LightTransfer) != 0)
            return false;

        if (!currentLightSector3D.IsSolid && currentLightSector3D.IsLightTransfer && nextSector3D.IsSolid)
            return true;

        if (currentLightSector3D.IsSolid && !nextSector3D.IsSolid)
            return true;

        return false;
    }

    private static int HeightCompare(Sector3D x, Sector3D y)
    {
        if (y.ControlTop.Z == x.ControlTop.Z)
            return x.ControlSector.Id.CompareTo(y.ControlSector.Id);

        return y.ControlTop.Z.CompareTo(x.ControlTop.Z);
    }

    // Returns the 3D sector that is valid for the viewer's current Z position.
    public static bool TryGetValidViewLightSector3D(Entity viewer, [NotNullWhen(true)] out Sector? lightSector3D)
    {
        var viewZ = viewer.Position.Z + viewer.ViewZ;
        for (int i = 0; i < viewer.Sector.SectorPlanes3D.Length - 1; i++)
        {
            ref var plane = ref viewer.Sector.SectorPlanes3D[i];
            ref var nextPlane = ref viewer.Sector.SectorPlanes3D[i + 1];
            if (viewZ > nextPlane.GetZ() && viewZ <= plane.GetZ())
            {
                lightSector3D = nextPlane.LightSector;
                return true;
            }
        }

        lightSector3D = null;
        return false;
    }

    public void Reset()
    {
        for (int i = 0; i < FakeSector.Lines.Length; i++)
            FakeSector.Lines[i].Front.Reset();

        FakeBottom.Reset(0);
        FakeTop.Reset(0);
        FakeSector.Reset();
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

    private Line[] CreateSector3DLines(IWorld world, Sector sector, Side controlSide, bool createBackSide)
    {
        var logicalWallLocation = GetLogicalWallLocation();
        var lines = new Line[sector.Lines.Length];

        for (int i = 0; i < sector.Lines.Length; i++)
        {
            var line = sector.Lines[i];
            if (line.Back == null || line.Front.Sector == line.Back.Sector)
            {
                lines[i] = NoRenderLine3D;
                continue;
            }

            var useSide = logicalWallLocation == WallLocation.Middle ? controlSide : line.Front.Sector == sector ? line.Back : line.Front;

            var controlWall = useSide.GetWall(logicalWallLocation);
            var middle = new SectorWall3D(controlWall, WallLocation.Middle3D);
            var side = new Side(world.Geometry.CreateNewSideId(), line.Front.Offset, EmptyWall, middle, EmptyWall, sector);
            // Normalize so front is always the rendered side
            var lineSeg = line.Segment;
            if (line.Front.Sector == sector)
                lineSeg = new(lineSeg.End, lineSeg.Start);

            var backMiddle = new SectorWall3D(controlWall, WallLocation.Middle3D);
            var backSide = createBackSide ? new Side(world.Geometry.CreateNewSideId(), line.Front.Offset, EmptyWall, backMiddle, EmptyWall, sector) : null;
            lines[i] = new Line(line.Id, lineSeg, side, backSide, default, LineSpecial.Default, default);
        }

        return lines;
    }

    public Entity GetSectorEntity3D()
    {
        Entity.PrevPosition.Z = ControlBottom.PrevZ;
        Entity.Position.Z = ControlBottom.Z;
        Entity.Height = MathHelper.Max(ControlTop.Z - ControlBottom.Z, 0);
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

    public void CalculateHeights()
    {
        WallHeights = new WallHeights(ControlTop.Z, ControlBottom.Z, ControlTop.PrevZ, ControlBottom.PrevZ);
        WallHeightsUnclipped = WallHeights;
        ClipBottomZ = ControlBottom.Z;
        ClipPrevBottomZ = ControlBottom.Z;

        if (ParentSector.Sectors3D.Length == 0)
            return;

        for (int i = 0; i < ParentSector.Sectors3D.Length; i++)
        {
            var checkSector3D = ParentSector.Sectors3D[i];
            if (checkSector3D == this)
            {
                if (i < ParentSector.Sectors3D.Length - 1)
                {
                    checkSector3D = ParentSector.Sectors3D[i + 1];
                    if (checkSector3D.ControlTop.Z > WallHeights.BottomZ)
                    {
                        WallHeights.SetClipped = true;
                        if (ShouldClipSector3D(checkSector3D, true))
                        {
                            WallHeights.Clipped = true;
                            WallHeights.BottomZ = checkSector3D.ControlTop.Z;
                            WallHeights.PrevBottomZ = checkSector3D.ControlTop.Z;
                            ClipBottomZ = checkSector3D.ControlTop.Z;
                            ClipPrevBottomZ = checkSector3D.ControlTop.PrevZ;
                        }
                    }
                }

                break;
            }

            if (ControlBottom.Z < checkSector3D.ControlTop.Z && ControlTop.Z > checkSector3D.ClipBottomZ && ShouldClipSector3D(checkSector3D, false))
            {
                if (!AdjustWallHeights(ref WallHeights,
                    checkSector3D.ControlTop.Z, checkSector3D.ClipBottomZ, checkSector3D.ControlTop.PrevZ, checkSector3D.ClipPrevBottomZ))
                    break;
            }
        }

        FakeTop.Z = WallHeights.TopZ;
        FakeTop.PrevZ = WallHeights.PrevTopZ;
        FakeBottom.Z = WallHeights.BottomZ;
        FakeBottom.PrevZ = WallHeights.PrevBottomZ;

        if (FakeTopFlipped != null)
        {
            FakeTopFlipped.Z = WallHeights.TopZ;
            FakeTopFlipped.PrevZ = WallHeights.PrevTopZ;
        }

        if (FakeBottomFlipped != null)
        {
            FakeBottom.Z = WallHeights.BottomZ;
            FakeBottom.PrevZ = WallHeights.PrevBottomZ;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref WallHeights GetWallHeights()
    {
        if (RenderDataStyle == RenderDataStyle.Normal)
            return ref WallHeights;
        return ref WallHeightsUnclipped;
    }

    public bool CalculateWallHeights(Side side, out WallHeights newWallHeights)
    {
        newWallHeights = GetWallHeights();
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
                if (WallVerticesOccluded(ref newWallHeights, wall))
                    return false;

                if (!AdjustWallHeights(ref newWallHeights, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ))
                    return false;
            }

            if (GeometryRenderer.UpperIsVisible(side, side.Sector, side.PartnerSide.Sector))
            {
                WorldTriangulator.HandleTwoSidedUpper(side, side.Sector.Ceiling, side.PartnerSide.Sector.Ceiling, default, true, ref wall, calculateUV: false);
                if (WallVerticesOccluded(ref newWallHeights, wall))
                    return false;

                if (!AdjustWallHeights(ref newWallHeights, wall.TopLeft.Z, wall.BottomRight.Z, wall.TopLeft.PrevZ, wall.BottomRight.PrevZ))
                    return false;
            }
        }

        // Only clip if this side has 3D sectors. Otherwise overlapping non-solid sectors won't render.
        if (side.PartnerSide == null || side.PartnerSide.Sector.Sectors3D.Length > 0)
        {
            if (!AdjustWallHeights3D(side.Sector, ref newWallHeights, false))
                return false;
        }

        if (side.PartnerSide != null && side.Sector.Sectors3D.Length > 0)
        {
            if (!AdjustWallHeights3D(side.PartnerSide.Sector, ref newWallHeights, false))
                return false;
        }

        return true;
    }

    private static bool WallVerticesOccluded(ref WallHeights newWallHeights, in WallVertices wall)
    {
        newWallHeights.Invalid = wall.TopLeft.Z >= newWallHeights.TopZ && wall.BottomRight.Z <= newWallHeights.BottomZ;
        return newWallHeights.Invalid;
    }

    private bool AdjustWallHeights3D(Sector sector, ref WallHeights newWallHeights, bool clipOtherSolid)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3D = sector.Sectors3D[i];
            if (!ShouldClipSector3D(sector3D, true, clipOtherSolid))
                continue;

            if (!AdjustWallHeights(ref newWallHeights, sector3D.ControlTop.Z, sector3D.ControlBottom.PrevZ, sector3D.ControlTop.Z, sector3D.ControlBottom.PrevZ))
                return false;
        }

        return true;
    }

    private bool ShouldClipSector3D(Sector3D other, bool clipSolid = false, bool clipOtherSolid = true)
    {
        if (other == this)
            return false;

        if (RenderDataStyle != RenderDataStyle.Normal && other.RenderDataStyle != RenderDataStyle.Normal)
        {
            if ((Alpha == 0 && other.Alpha != 0) || (Alpha != 0 && other.Alpha == 0))
                return false;

            return true;
        }

        if (ClipStyle != other.ClipStyle && (IsLightTransfer || other.IsLightTransfer))
            return false;

        if (clipOtherSolid && ClipStyle == ClipStyle.NotSolid && other.ClipStyle == ClipStyle.Solid)
            return true;

        var currentSolid = Flags & SectorFlags3D.Solid;
        var otherSolid = other.Flags & SectorFlags3D.Solid;
        if (clipSolid && currentSolid != 0 && otherSolid != 0)
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
            wallHeights.BottomZ = InvalidZ;
            wallHeights.TopZ = InvalidZ;
            wallHeights.PrevTopZ = InvalidZ;
            wallHeights.PrevBottomZ = InvalidZ;
            return false;
        }

        return true;
    }

    public WallLocation GetLogicalWallLocation()
    {
        if ((Flags & SectorFlags3D.UseParentUpperTexture) != 0)
            return WallLocation.Upper;
        if ((Flags & SectorFlags3D.UseParentLowerTexture) != 0)
            return WallLocation.Lower;
        return WallLocation.Middle;
    }

    public override string ToString() => $"3D Sector={SectorId} ControlId={ControlSector.Id} ParentId={ParentSectorId} Flags={Flags} LightLevel={ControlSector.LightLevel} Style={RenderDataStyle} [{ControlSector.Floor.Z} -> {ControlSector.Ceiling.Z}]{ControlSector.ToStringColors()}";
}
