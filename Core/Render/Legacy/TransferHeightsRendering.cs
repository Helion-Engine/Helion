using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;

namespace Helion.Render.Legacy;

public enum TransferHeightPos
{
    None,
    Top,
    Middle,
    Bottom,
}

public static class TransferHeightsRendering
{
    public static TransferHeightPos GetPos(Sector control, in Vec3D viewPosition)
    {
        if (viewPosition.Z > control.Ceiling.Z)
            return TransferHeightPos.Top;
        if (viewPosition.Z > control.Floor.Z)
            return TransferHeightPos.Middle;
        return TransferHeightPos.Bottom;
    }

    public static bool ShouldRenderWalls(TransferHeightPos pos, Sector sector, Sector control)
    {
        if (pos == TransferHeightPos.Bottom)
            return true;

        return sector.Floor.Z < control.Floor.Z;
    }

    public static short GetSectorLightLevel(Sector sector, TransferHeightPos pos)
    {
        if (sector.TransferHeights != null && pos != TransferHeightPos.Middle)
            return sector.TransferHeights.ControlSector.LightLevel;

        return sector.LightLevel;
    }
}
