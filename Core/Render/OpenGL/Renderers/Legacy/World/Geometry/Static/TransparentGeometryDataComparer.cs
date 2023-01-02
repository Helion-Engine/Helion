using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

internal class TransparentGeometryDataComparer : IComparer<GeometryData>
{
    public int Compare(GeometryData x, GeometryData y)
    {
        if (x.Texture.TransparentPixelCount == y.Texture.TransparentPixelCount)
            return x.Texture.TextureId.CompareTo(y.Texture.TextureId);

        return x.Texture.TransparentPixelCount.CompareTo(y.Texture.TransparentPixelCount);
    }
}
