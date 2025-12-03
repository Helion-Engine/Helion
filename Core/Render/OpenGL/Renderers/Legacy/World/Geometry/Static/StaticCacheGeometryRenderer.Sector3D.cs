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
    private readonly Action<Side, Wall, GLLegacyTexture, DynamicVertex[]>? m_renderSectorWallVertices3D;

    private void AddSectors3D(Sector sector, bool update)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
            AddSector3D(sector.Sectors3D[i], SectorPlanes.Floor | SectorPlanes.Ceiling, update);
    }

    private void AddSector3D(Sector3D sector3d, SectorPlanes planes, bool update)
    {
        var saveTransfer = sector3d.ParentSector.TransferFloorLightSector;
        sector3d.ParentSector.TransferFloorLightSector = sector3d.ParentSector;

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

        sector3d.ParentSector.TransferFloorLightSector = saveTransfer;

        if (!sector3d.ShouldRenderWalls)
            return;

        var wallHeights = GeometryRenderer.SetSectorForLineRendering3D(sector3d);
        for (int i = 0; i < sector3d.FakeSector.Lines.Length; i++)
        {
            var sectorLine = sector3d.FakeSector.Lines[i];
            var side = sectorLine.Front;
            var dynamic = side.IsDynamic || sector3d.ControlSector.IsMoving;
            if (dynamic && (sector3d.ControlSector.Floor.Dynamic == SectorDynamic.Movement || sector3d.ControlSector.Ceiling.Dynamic == SectorDynamic.Movement))
                continue;

            m_geometryRenderer.RenderSectorLine3D(sector3d, i, true, true, wallHeights, m_renderSectorWallVertices3D);
        }
    }

    private void RenderSectorWallVertices3D(Side side, Wall wall, GLLegacyTexture texture, DynamicVertex[] vertices)
    {
        UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, vertices, null, side, wall, true, texture);
    }
}
