using System.Collections.Generic;
using Helion.Maps.Geometry;

namespace Helion.Maps.Special.Specials
{
    public class DonutSpecial
    {
        public static List<Sector>? GetDonutSectors(Sector start)
        {
            List<Sector> sectors = new List<Sector>();
            sectors.Add(start);
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
            if (sector.Lines.Count > 0 && sector.Lines[0].TwoSided)
            {
                if (sector.Lines[0].Front.Sector == sector)
                    return sector.Lines[0].Back.Sector;
                else
                    return sector.Lines[0].Front.Sector;
            }

            return null;
        }

        private static Sector? GetDestSector(Sector startSector, Sector raiseSector)
        {
            foreach (var line in raiseSector.Lines)
            {
                if (line.Back != null && line.Back.Sector != startSector)
                    return line.Back.Sector;
            }

            return null;
        }
    }
}
