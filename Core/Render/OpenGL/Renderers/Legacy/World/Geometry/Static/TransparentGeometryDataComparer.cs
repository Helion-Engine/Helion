using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

internal class TransparentGeometryDataComparer : IComparer<GeometryData>
{
    public int Compare(GeometryData x, GeometryData y)
    {
        if (x.Texture.HasTransparentPixels == y.Texture.HasTransparentPixels)
            return x.Texture.TextureId.CompareTo(y.Texture.TextureId);

        return x.Texture.HasTransparentPixels.CompareTo(y.Texture.HasTransparentPixels);
    }
}
