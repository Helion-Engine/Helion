using Helion.World.Static;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

internal class GeometryIndexComparer : IComparer<StaticGeometryData>
{
    public int Compare(StaticGeometryData x, StaticGeometryData y)
    {
        return x.GeometryDataStartIndex.CompareTo(y.GeometryDataStartIndex);
    }
}
