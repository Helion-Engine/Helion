using Helion.Geometry.Boxes;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using System.Collections.Generic;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A collection of lines and sectors that are reachable from each other by
/// traversing adjacent subsectors.
/// </summary>
public class Island
{
    public readonly int Id;
    public readonly List<BspSubsector> Subsectors = new();
    public readonly List<int> LineIds = new();
    public bool IsMonsterCloset;
    public bool IsVooDooCloset;
    public int InitialMonsterCount;
    public Box2D Box;

    public Island(int id)
    {
        Id = id;
    }

    // Box is contained in this island box. Does not include where min or max are equal.
    public bool Contains(in Box2D box) => Box.Contains(box.Min) && Box.Contains(box.Max);

    // Box is contained in this island box. Allows inclusive checks where min or max are equal.
    public bool ContainsInclusive(in Box2D box) => Box.ContainsInclusive(box.Min) && Box.ContainsInclusive(box.Max);

    public bool OnRightOfSectorLines(IList<Line> lines, int sectorId, in Box2D box)
    {
        for (int i = 0; i < LineIds.Count; i++)
        {
            var lineId = LineIds[i];
            if (lineId < 0 || lineId >= lines.Count)
                continue;

            var line = lines[lineId];
            if (line.Back != null && ReferenceEquals(line.Front.Sector, line.Back.Sector))
                continue;

            bool onRight = line.Front.Sector.Id == sectorId;
            if (!line.Segment.OnRight(box.Min) != onRight || !line.Segment.OnRight(box.Max))
                return false;
        }

        return true;
    }
}
