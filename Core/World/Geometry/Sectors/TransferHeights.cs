namespace Helion.World.Geometry.Sectors;

public class TransferHeights
{
    public readonly Sector ParentSector;
    public readonly Sector ControlSector;

    // Because sectors are classes returning new ones every time would put too much pressure on the GC.
    // This static rotating list will allow for up to 16 calls in the stack before they are reused.
    // A rendering function will generally call this twice, so this gives 8 stacked calls which should be more than enough.
    private static readonly Sector[] RenderSectors = new Sector[]
    {   Sector.CreateDefault(), Sector.CreateDefault(),
        Sector.CreateDefault(), Sector.CreateDefault(),
        Sector.CreateDefault(), Sector.CreateDefault(),
        Sector.CreateDefault(), Sector.CreateDefault(),
        Sector.CreateDefault(), Sector.CreateDefault(),
        Sector.CreateDefault(), Sector.CreateDefault(),
        Sector.CreateDefault(), Sector.CreateDefault(),
        Sector.CreateDefault(), Sector.CreateDefault(),
    };

    private static int RenderSectorIndex = RenderSectors.Length;

    public TransferHeights(Sector parentSector, Sector controlSector)
    {
        ParentSector = parentSector;
        ControlSector = controlSector;
    }

    public Sector GetRenderSector(TransferHeightView view)
    {
        Sector sector = RenderSectors[++RenderSectorIndex % RenderSectors.Length];
        switch (view)
        {
            case TransferHeightView.Top:
                sector.Id = ControlSector.Id;
                sector.Ceiling.Plane = ParentSector.Ceiling.Plane;
                sector.Ceiling.Z = ParentSector.Ceiling.Z;
                sector.Ceiling.PrevZ = ParentSector.Ceiling.PrevZ;
                sector.Ceiling.TextureHandle = ControlSector.Ceiling.TextureHandle;
                sector.Ceiling.LightLevel = ControlSector.CeilingRenderLightLevel;
                sector.Ceiling.Sector = ControlSector;
                sector.LightLevel = ControlSector.LightLevel;

                sector.Floor.Plane = ControlSector.Ceiling.Plane;
                sector.Floor.Z = ControlSector.Ceiling.Z;
                sector.Floor.PrevZ = ControlSector.Ceiling.PrevZ;
                sector.Floor.TextureHandle = ControlSector.Floor.TextureHandle;
                sector.Floor.LightLevel = ControlSector.FloorRenderLightLevel;
                sector.Floor.Sector = ControlSector;
                sector.LightLevel = ControlSector.LightLevel;
                break;

            case TransferHeightView.Middle:
                sector.Id = ParentSector.Id;
                sector.Ceiling.Plane = ControlSector.Ceiling.Plane;
                sector.Ceiling.Z = ControlSector.Ceiling.Z;
                sector.Ceiling.PrevZ = ControlSector.Ceiling.PrevZ;
                sector.Ceiling.TextureHandle = ParentSector.Ceiling.TextureHandle;
                sector.Ceiling.LightLevel = ParentSector.CeilingRenderLightLevel;
                sector.Ceiling.Sector = ParentSector;
                sector.LightLevel = ParentSector.LightLevel;

                sector.Floor.Plane = ControlSector.Floor.Plane;
                sector.Floor.Z = ControlSector.Floor.Z;
                sector.Floor.PrevZ = ControlSector.Floor.PrevZ;
                sector.Floor.TextureHandle = ParentSector.Floor.TextureHandle;
                sector.Floor.LightLevel = ParentSector.FloorRenderLightLevel;
                sector.Floor.Sector = ParentSector;
                sector.LightLevel = ParentSector.LightLevel;
                break;

            default:
                sector.Id = ControlSector.Id;
                sector.Ceiling.Plane = ControlSector.Floor.Plane;
                sector.Ceiling.Z = ControlSector.Floor.Z;
                sector.Ceiling.PrevZ = ControlSector.Floor.PrevZ;
                sector.Ceiling.TextureHandle = ControlSector.Ceiling.TextureHandle;
                sector.Ceiling.LightLevel = ControlSector.CeilingRenderLightLevel;
                sector.Ceiling.Sector = ControlSector;
                sector.LightLevel = ControlSector.LightLevel;

                sector.Floor.Plane = ParentSector.Floor.Plane;
                sector.Floor.Z = ParentSector.Floor.Z;
                sector.Floor.PrevZ = ParentSector.Floor.PrevZ;
                sector.Floor.TextureHandle = ControlSector.Floor.TextureHandle;
                sector.Floor.LightLevel = ControlSector.FloorRenderLightLevel;
                sector.Floor.Sector = ControlSector;
                sector.LightLevel = ControlSector.LightLevel;
                break;
        }

        sector.DataChanges = ParentSector.DataChanges | ControlSector.DataChanges;
        sector.TransferFloorLightSector = ParentSector.TransferFloorLightSector;
        sector.TransferCeilingLightSector = ParentSector.TransferCeilingLightSector;
        return sector;
    }

    public static TransferHeightView GetView(Sector viewSector, double viewZ)
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
