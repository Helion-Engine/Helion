using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Subsectors;
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
    public bool Flood;
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

    public bool BoxInsideSector(in Box2D box)
    {
        if (box.Min.X == box.Max.X || box.Min.Y == box.Max.Y)
            return false;

        bool hitBottomLeft = false;
        bool hitBottomRight = false;
        bool hitTopLeft = false;
        bool hitTopRight = false;

        var bottomLeft = box.BottomLeft;
        var bottomRight = box.BottomRight;
        var topLeft = box.TopLeft;
        var topRight = box.TopRight;

        foreach (var subsector in Subsectors)
        {
            if (!hitBottomLeft && subsector.Box.ContainsInclusive(bottomLeft))
                hitBottomLeft = true;
            if (!hitBottomRight && subsector.Box.ContainsInclusive(bottomRight))
                hitBottomRight = true;
            if (!hitTopLeft && subsector.Box.ContainsInclusive(topLeft))
                hitTopLeft = true;
            if (!hitTopRight && subsector.Box.ContainsInclusive(topRight))
                hitTopRight = true;

            if (hitBottomLeft && hitBottomRight && hitTopLeft && hitTopRight)
                return true;
        }

        return false;
    }
}
