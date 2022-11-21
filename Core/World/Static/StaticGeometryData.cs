using Helion.Render.Legacy.Renderers.World.Geometry.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.World.Static;

public struct StaticGeometryData
{
    public GeometryData? GeometryData;
    public int GeometryDataStartIndex;
    public int GeometryDataLength;

    public StaticGeometryData(GeometryData? geometryData, int geometryDataStartIndex, int geometryDataLength)
    {
        GeometryData = geometryData;
        GeometryDataStartIndex = geometryDataStartIndex;
        GeometryDataLength = geometryDataLength;
    }
}
