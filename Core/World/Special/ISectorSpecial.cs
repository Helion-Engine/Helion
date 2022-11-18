using Helion.World.Geometry.Sectors;
using System.Collections;
using System.Collections.Generic;

namespace Helion.World.Special;

public interface ISectorSpecial : ISpecial
{
    Sector Sector { get; set; }
    void FinalizeDestroy();
    void Pause();
    void Resume();
    bool IsPaused { get; }
    bool MultiSector { get; }
    IEnumerable<(Sector, SectorPlane)> GetSectors();
}
