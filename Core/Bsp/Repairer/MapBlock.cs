using System.Collections.Generic;
using Helion.Bsp.Geometry;

namespace Helion.Bsp.Repairer;

public class MapBlock
{
    public readonly List<BspSegment> Segments = new List<BspSegment>();
    public readonly List<BspSegment> OneSidedSegments = new List<BspSegment>();
}

