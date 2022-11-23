using Helion.World.Static;
namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

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
