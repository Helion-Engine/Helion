using Helion.Render.OpenGL.Texture.Legacy;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Static;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public partial class StaticCacheGeometryRenderer
{
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderOneSidedSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedLowerSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedUpperSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedMiddleSliceFunc;
    private readonly Action<Side, Wall, Sector, GLLegacyTexture?, Span<DynamicVertex>, Sector3D?>? m_renderSectorWallVertices3D;

    private void AddSectors3D(Sector sector, bool update)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
            AddSector3D(sector.Sectors3D[i], SectorPlanes.Floor | SectorPlanes.Ceiling, update);
    }

    private void AddSector3D(Sector3D sector3D, SectorPlanes planes, bool update, bool clearWallVertices = false)
    {
        AddSectorPlanes3D(sector3D, planes, update);

        if (!sector3D.ShouldRenderWalls)
            return;

        m_geometryRenderer.SetSectorForLineRendering3D(sector3D);
        for (int i = 0; i < sector3D.FakeSector.Lines.Length; i++)
        {
            var sectorLine = sector3D.FakeSector.Lines[i];
            var side = sectorLine.Front;
            var dynamic = side.IsDynamic || sector3D.ControlSector.IsMoving;
            if (dynamic && (sector3D.ControlSector.Floor.Dynamic == SectorDynamic.Movement || sector3D.ControlSector.Ceiling.Dynamic == SectorDynamic.Movement))
                continue;

            if (clearWallVertices)
            {
                var wall = sectorLine.Front.Middle;
                ClearSideGeometryVertices(sectorLine.Front, wall);

                if (wall.Static.GeometryData != null)
                    m_freeManager.Add(wall.Static, m_geometryRenderer.GetWallType(sectorLine.Front, wall, sector3D), GetRepeatY(sectorLine.Front, wall));

                wall.Static.GeometryData = null;
            }

            m_geometryRenderer.RenderSectorLine3D(sector3D, i, true, true, m_renderSectorWallVertices3D);
        }
    }

    // Ceiling = ControlTop
    // Floor = ControlBottom
    private void AddSectorPlanes3D(Sector3D sector3D, SectorPlanes planes, bool update)
    {
        planes &= sector3D.RenderPlanes;
        if (!sector3D.ShouldRenderFlats || planes == SectorPlanes.None)
            return;

        var saveTransfer = sector3D.ParentSector.TransferFloorLightSector;
        sector3D.ParentSector.TransferFloorLightSector = sector3D.ParentSector;

        if ((planes & SectorPlanes.Ceiling) != 0)
        {
            // Render FakeBottom at ControlTop
            AddSectorPlane(sector3D.ParentSector, sector3D.ControlTop.Facing, floor: true, update: update, renderSector: sector3D.ControlSector,
                lightLevelSector: sector3D.LightTop, geometryPlane: sector3D.FakeBottom, allowAlpha: true, sector3D: sector3D);

            if (sector3D.FakeBottomFlipped != null)
            {
                AddSectorPlane(sector3D.ParentSector, sector3D.ControlTop.Facing, floor: false, update: update, renderSector: sector3D.ControlSector,
                    lightLevelSector: sector3D.LightTop, geometryPlane: sector3D.FakeBottomFlipped, allowAlpha: true, sector3D: sector3D);
            }
        }

        if ((planes & SectorPlanes.Floor) != 0)
        {
            // Render FakeTop at ControlBottom
            AddSectorPlane(sector3D.ParentSector, sector3D.ControlBottom.Facing, floor: false, update: update, renderSector: sector3D.ControlSector,
                lightLevelSector: sector3D.LightBottom, geometryPlane: sector3D.FakeTop, allowAlpha: true, sector3D: sector3D);

            if (sector3D.FakeTopFlipped != null)
            {
                AddSectorPlane(sector3D.ParentSector, sector3D.ControlBottom.Facing, floor: true, update: update, renderSector: sector3D.ControlSector,
                    lightLevelSector: sector3D.LightBottom, geometryPlane: sector3D.FakeTopFlipped, allowAlpha: true, sector3D: sector3D);
            }
        }

        sector3D.ParentSector.TransferFloorLightSector = saveTransfer;
    }

    private void RenderSectorWallVertices3D(Side side, Wall wall, Sector wallSector, GLLegacyTexture? texture, Span<DynamicVertex> vertices, Sector3D? sector3D)
    {
        UpdateVertices(ref wall.Static, wall.TextureHandle, vertices, null, side, wall, true, texture, sector3D);
    }
}
