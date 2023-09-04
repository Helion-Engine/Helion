using System.Diagnostics;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;

namespace Helion.RenderNew.Textures;

public class TextureAtlas
{
    public readonly Dimension Dimension;
    public readonly Vec2F UVFactor;
    
    public TextureAtlas(Dimension dimension)
    {
        Debug.Assert(dimension.HasPositiveArea, "Texture atlas needs positive dimensions");

        Dimension = dimension;
        UVFactor = dimension.Vector.Float.Inverse();
    }

    public bool TryAllocate(Dimension dim, out Box2I area)
    {
        // TODO
        area = dim.Box;
        return true;
    }
}