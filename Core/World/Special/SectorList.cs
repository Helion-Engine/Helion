using Helion.World.Geometry.Sectors;
using System;
using System.Collections.Generic;

namespace Helion.World.Special;

public readonly ref struct SectorList
{
    public SectorList(IList<Sector> sectors)
    {
        Sectors = sectors;
    }

    public SectorList(Sector sector)
    {
        Sector = sector;
    }

    public int Count => Sectors == null ? 1 : Sectors.Count;
    public Sector GetSector(int index)
    {
        if (Sectors == null)
            return Sector;
        return Sectors[index];
    }

    public readonly IList<Sector>? Sectors;
    public readonly Sector? Sector;
}