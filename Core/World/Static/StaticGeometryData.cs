using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.World.Static;

public struct StaticGeometryData
{
    public GeometryData? GeometryData;
    public int Index;
    public int Length;

    public StaticGeometryData(GeometryData? geometryData, int index, int length)
    {
        GeometryData = geometryData;
        Index = index;
        Length = length;
    }
}
