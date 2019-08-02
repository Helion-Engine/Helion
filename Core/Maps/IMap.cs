using System.Collections.Generic;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Maps.Things;
using Helion.Util;

namespace Helion.Maps
{
    public interface IMap
    {
        CIString Name { get; }

        MapType MapType { get; }

        IList<Line> Lines { get; }
        
        IList<Thing> Things { get; }
        
        IList<Side> Sides { get; }
        
        IList<Sector> Sectors { get; }
        
        IList<SectorFlat> SectorFlats { get; }
        
        IList<Vertex> Vertices { get; }
    }
}