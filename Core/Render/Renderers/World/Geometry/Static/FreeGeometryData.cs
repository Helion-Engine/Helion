using Helion;
using Helion.Render;
using Helion.Render.Renderers.World.Geometry.Static;
using Helion.World.Static;
namespace Helion.Render.Renderers.World.Geometry.Static;

public readonly struct FreeGeometryData
{
    public readonly int TextureHandle;
    public readonly StaticGeometryData GeometryData;

    public FreeGeometryData(int textureHandle, StaticGeometryData geometryData)
    {
        TextureHandle = textureHandle;
        GeometryData = geometryData;
    }
}
