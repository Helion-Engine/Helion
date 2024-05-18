using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.World.Special.Specials;

public static class DoorLight
{
    public static void UpdateLight(IWorld world, int lightTag, double doorTop, double doorBottom, double doorZ)
    {
        double fullOpening = doorTop - doorBottom;
        if (fullOpening <= 0)
            return;
        double currentOpening = doorZ - doorBottom;
        double percentage = Math.Clamp(currentOpening / fullOpening, 0, short.MaxValue);
        var sectors = world.FindBySectorTag(lightTag);
        for (int i = 0; i < sectors.Count; i++)
        {
            var sector = sectors[i];
            short max = 0;
            short min = sector.LightLevel;
            for (int j = 0; j < sector.Lines.Count; j++)
            {
                var nextSector = GetNextSector(sector.Lines[j], sector);
                if (nextSector == null)
                    continue;
                if (nextSector.LightLevel > max)
                    max = nextSector.LightLevel;
                if (nextSector.LightLevel < min)
                    min = nextSector.LightLevel;
            }
            world.SetSectorLightLevel(sector, (short)Math.Clamp((min + (max - min) * percentage), min, max));
        }
    }

    private static Sector? GetNextSector(Line line, Sector sector)
    {
        if (line.Front.Sector == sector)
        {
            if (line.Back != null && line.Back.Sector != sector)
                return line.Back.Sector;
            return null;
        }
        return line.Front.Sector;
    }
}
