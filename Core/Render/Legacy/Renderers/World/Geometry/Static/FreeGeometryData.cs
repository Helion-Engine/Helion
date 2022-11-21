using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.World.Geometry.Static;
using Helion.World.Static;
namespace Helion.Render.Legacy.Renderers.World.Geometry.Static;

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
