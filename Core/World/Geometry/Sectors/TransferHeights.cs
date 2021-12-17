using Helion.Maps.Specials;
using Helion.World.Geometry.Subsectors;

namespace Helion.World.Geometry.Sectors;

public class TransferHeights
{
    private enum TransferHeightView
    {
        None,
        Top,
        Middle,
        Bottom,
    }

    public readonly Sector ParentSector;
    public readonly Sector ControlSector;

    private readonly Sector m_renderSector;

    public TransferHeights(Sector parentSector, Sector controlSector)
    {
        ParentSector = parentSector;
        ControlSector = controlSector;
        m_renderSector = new Sector(0, 0, 0, 
            new SectorPlane(0, SectorPlaneFace.Floor, 0, 0, 0), 
            new SectorPlane(0, SectorPlaneFace.Ceiling, 0, 0, 0),
            Maps.Specials.ZDoom.ZDoomSectorSpecialType.None, SectorData.Default);
    }

    public Sector GetRenderSector(Sector viewSector, double viewZ)
    {
        switch (GetView(viewSector, viewZ))
        {
            case TransferHeightView.Top:
                m_renderSector.Ceiling.Z = ParentSector.Ceiling.Z;
                m_renderSector.Ceiling.PrevZ = ParentSector.Ceiling.PrevZ;
                m_renderSector.Ceiling.TextureHandle = ControlSector.Ceiling.TextureHandle;
                m_renderSector.Ceiling.LightLevel = ControlSector.CeilingRenderLightLevel;
                m_renderSector.Ceiling.Sector = ControlSector;
                m_renderSector.LightLevel = ControlSector.LightLevel;

                m_renderSector.Floor.Z = ControlSector.Ceiling.Z;
                m_renderSector.Floor.PrevZ = ControlSector.Ceiling.PrevZ;
                m_renderSector.Floor.TextureHandle = ControlSector.Floor.TextureHandle;
                m_renderSector.Floor.LightLevel = ControlSector.FloorRenderLightLevel;
                m_renderSector.Floor.Sector = ControlSector;
                m_renderSector.LightLevel = ControlSector.LightLevel;
                break;

            case TransferHeightView.Middle:
                m_renderSector.Ceiling.Z = ControlSector.Ceiling.Z;
                m_renderSector.Ceiling.PrevZ = ControlSector.Ceiling.PrevZ;
                m_renderSector.Ceiling.TextureHandle = ParentSector.Ceiling.TextureHandle;
                m_renderSector.Ceiling.LightLevel = ParentSector.CeilingRenderLightLevel;
                m_renderSector.Ceiling.Sector = ParentSector;
                m_renderSector.LightLevel = ParentSector.LightLevel;

                m_renderSector.Floor.Z = ControlSector.Floor.Z;
                m_renderSector.Floor.PrevZ = ControlSector.Floor.PrevZ;
                m_renderSector.Floor.TextureHandle = ParentSector.Floor.TextureHandle;
                m_renderSector.Floor.LightLevel = ParentSector.FloorRenderLightLevel;
                m_renderSector.Floor.Sector = ParentSector;
                m_renderSector.LightLevel = ParentSector.LightLevel;
                break;

            default:
                m_renderSector.Ceiling.Z = ControlSector.Floor.Z;
                m_renderSector.Ceiling.PrevZ = ControlSector.Floor.PrevZ;
                m_renderSector.Ceiling.TextureHandle = ControlSector.Ceiling.TextureHandle;
                m_renderSector.Ceiling.LightLevel = ControlSector.CeilingRenderLightLevel;
                m_renderSector.Ceiling.Sector = ControlSector;
                m_renderSector.LightLevel = ControlSector.LightLevel;

                m_renderSector.Floor.Z = ParentSector.Floor.Z;
                m_renderSector.Floor.PrevZ = ParentSector.Floor.PrevZ;
                m_renderSector.Floor.TextureHandle = ControlSector.Floor.TextureHandle;
                m_renderSector.Floor.LightLevel = ControlSector.FloorRenderLightLevel;
                m_renderSector.Floor.Sector = ControlSector;
                m_renderSector.LightLevel = ControlSector.LightLevel;
                break;
        }

        m_renderSector.DataChanges = ParentSector.DataChanges | ControlSector.DataChanges;
        return m_renderSector;
    }

    private static TransferHeightView GetView(Sector viewSector, double viewZ)
    {
        // Transfer heights works off the TransferHeights of the sector that the player is viewing from
        // This means that it does the calculations for what sector to rendering based on the TransferHeights of the sector you are viewing from...
        if (viewSector.TransferHeights == null)
            return TransferHeightView.Middle;

        if (viewZ > viewSector.TransferHeights.ControlSector.Ceiling.Z)
            return TransferHeightView.Top;
        if (viewZ > viewSector.TransferHeights.ControlSector.Floor.Z)
            return TransferHeightView.Middle;
        return TransferHeightView.Bottom;
    }
}
