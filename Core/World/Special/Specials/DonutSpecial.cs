using System.Collections.Generic;
using System.Linq;
using Helion.Util.Extensions;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class DonutSpecial
    {
        public static List<Sector>? GetDonutSectors(Sector start)
        {
            List<Sector> sectors = new List<Sector> { start };
            
            Sector? raiseSector = GetRaiseSector(start);
            if (raiseSector == null)
                return null;
            sectors.Add(raiseSector);

            Sector? destSector = GetDestSector(start, raiseSector);
            if (destSector == null)
                return null;
            sectors.Add(destSector);

            return sectors;
        }

        private static Sector? GetRaiseSector(Sector sector)
        {
            if (sector.Lines.Empty())
                return null;

            Line line = sector.Lines.First();
            if (line.Back == null)
                return null;
            
            return line.Front.Sector == sector ? line.Back.Sector : line.Front.Sector;
        }

        private static Sector? GetDestSector(Sector startSector, Sector raiseSector)
        {
            for (int i = 0; i < raiseSector.Lines.Count; i++)
            {
                Line line = raiseSector.Lines[i];
                if (line.Back != null && line.Back.Sector != startSector)
                    return line.Back.Sector;
            }

            return null;
        }
    }
}
