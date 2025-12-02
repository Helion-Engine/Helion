using Helion.World.Geometry.Sectors;
using Helion.World.Static;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public partial class StaticCacheGeometryRenderer
{
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderOneSidedSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedLowerSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedUpperSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedMiddleSliceFunc;
    private readonly Func<Sector3D, bool> m_shouldClipSector3D;

    private Sector3D m_currentSector3D = null!;

    private void AddSectors3D(Sector sector, bool update)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
            AddSector3D(sector.Sectors3D[i], SectorPlanes.Floor | SectorPlanes.Ceiling, update);
    }

    private void AddSector3D(Sector3D sector3d, SectorPlanes planes, bool update)
    {
        if ((planes & SectorPlanes.Floor) != 0)
        {
            AddSectorPlane(sector3d.ParentSector, sector3d.ControlTop.Facing, floor: true, update: update, renderSector: sector3d.ControlSector,
                lightLevelSector: sector3d.LightTop, geometryPlane: sector3d.FakeBottom, allowAlpha: true);

            if (sector3d.FakeBottomFlipped != null)
            {
                AddSectorPlane(sector3d.ParentSector, sector3d.ControlTop.Facing, floor: false, update: update, renderSector: sector3d.ControlSector,
                    lightLevelSector: sector3d.LightTop, geometryPlane: sector3d.FakeBottomFlipped, allowAlpha: true);
            }
        }

        if ((planes & SectorPlanes.Ceiling) != 0)
        {
            AddSectorPlane(sector3d.ParentSector, sector3d.ControlBottom.Facing, floor: false, update: update, renderSector: sector3d.ControlSector,
                lightLevelSector: sector3d.LightBottom, geometryPlane: sector3d.FakeTop, allowAlpha: true);

            if (sector3d.FakeTopFlipped != null)
            {
                AddSectorPlane(sector3d.ParentSector, sector3d.ControlBottom.Facing, floor: true, update: update, renderSector: sector3d.ControlSector,
                    lightLevelSector: sector3d.LightBottom, geometryPlane: sector3d.FakeTopFlipped, allowAlpha: true);
            }
        }

        if (sector3d.ShouldRenderWalls)
            RenderSectorLines3D(sector3d);
    }

    private void RenderSectorLines3D(Sector3D sector3d)
    {
        var wallHeights = sector3d.CalculateWallHeights();
        var newWallHeights = wallHeights;
        var wallSector = sector3d.FakeSector;

        m_currentSector3D = sector3d;

        for (int i = 0; i < sector3d.FakeSector.Lines.Length; i++)
        {
            var sectorLine = sector3d.FakeSector.Lines[i];
            var parentSectorLine = sector3d.ParentSector.Lines[i];
            var useSide = sectorLine.Front;
            var dynamic = useSide.IsDynamic || sector3d.ControlSector.IsMoving;
            if (dynamic && (sector3d.ControlSector.Floor.Dynamic == SectorDynamic.Movement || sector3d.ControlSector.Ceiling.Dynamic == SectorDynamic.Movement))
                continue;

            wallSector.Ceiling.Z = wallHeights.TopZ;
            wallSector.Floor.Z = wallHeights.BottomZ;

            bool flipped = parentSectorLine.Segment.Delta != sectorLine.Segment.Delta;
            var checkParentBack = flipped ? parentSectorLine.Back : parentSectorLine.Front;
            var checkParentFront = flipped ? parentSectorLine.Front : parentSectorLine.Back;

            if (checkParentBack != null)
            {
                Sector3D.CalculateWallHeights(checkParentBack, wallHeights, out newWallHeights, clipToSector3D: m_shouldClipSector3D);
                wallSector.Ceiling.Z = newWallHeights.TopZ;
                wallSector.Floor.Z = newWallHeights.BottomZ;
            }

            useSide.Middle.TextureHandle = sector3d.GetTextureHandle(useSide, checkParentBack);

            m_geometryRenderer.SetRenderOneSided(useSide);
            m_geometryRenderer.RenderOneSided(useSide, true, out var sideVertices, out _, out var texture,
                renderSector: wallSector, lightLevelSector: sector3d.ParentSector, renderSkySide: false, allowAlpha: true);

            if (sideVertices != null)
            {
                var wall = useSide.Middle;
                UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, sideVertices, null, useSide, wall, true, texture);
            }

            if (sector3d.ShouldRenderInsideWalls && sectorLine.Back != null &&
                (checkParentFront == null || Sector3D.CalculateWallHeights(checkParentFront, wallHeights, out newWallHeights, clipToSector3D: m_shouldClipSector3D)))
            {
                wallSector.Ceiling.Z = newWallHeights.TopZ;
                wallSector.Floor.Z = newWallHeights.BottomZ;

                useSide = sectorLine.Back;
                useSide.Middle.TextureHandle = sector3d.GetTextureHandle(useSide, checkParentFront);
                m_geometryRenderer.RenderOneSided(useSide, false, out sideVertices, out _, out texture,
                    renderSector: wallSector, lightLevelSector: sector3d.LightMiddle, renderSkySide: false, allowAlpha: true);

                if (sideVertices != null)
                {
                    var wall = useSide.Middle;
                    UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, sideVertices, null, useSide, wall, true, texture);
                }
            }
        }
    }

    private bool ShouldClipSector3D(Sector3D other)
    {
        if (other == m_currentSector3D)
            return false;

        var currentSolid = m_currentSector3D.Flags & SectorFlags3D.Solid;
        var otherSolid = other.Flags & SectorFlags3D.Solid;

        if (currentSolid != 0 && otherSolid != 0)
            return false;

        return currentSolid == otherSolid;
    }
}
