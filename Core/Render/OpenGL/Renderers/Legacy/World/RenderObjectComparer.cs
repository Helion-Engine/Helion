using Helion.World.Entities;
using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class RenderObjectComparer : IComparer<IRenderObject>
{
    public int Compare(IRenderObject? x, IRenderObject? y)
    {
        if (x == null || y == null)
            return 1;

        // Reverse distance order
        return y.RenderDistance.CompareTo(x.RenderDistance);
    }
}
