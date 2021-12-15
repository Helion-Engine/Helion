using Helion.World.Geometry.Lines;
using System.Collections.Generic;

namespace Helion.World.Geometry.Sectors;

public class TransferHeights
{
    public readonly Sector ControlSector;

    public TransferHeights(Sector controlSector)
    {
        ControlSector = controlSector;
    }

    public Sector? GetMinSector()
    {
        double min = double.MaxValue;
        Sector? minSector = null;

        for (int i = 0; i < ControlSector.Lines.Count; i++)
        {
            Line line = ControlSector.Lines[i];
            if (line.Front.Sector.Floor.Z < min)
            {
                min = line.Front.Sector.Floor.Z;
                minSector = line.Front.Sector;
            }

            if (line.Back != null && line.Back.Sector.Floor.Z < min)
            {
                min = line.Back.Sector.Floor.Z;
                minSector = line.Back.Sector;
            }
        }

        return minSector;
    }
}
