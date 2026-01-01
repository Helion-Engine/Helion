using Helion.World.Static;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public struct FreeGeometryData(int textureHandle, StaticGeometryData geometryData)
{
    public int TextureHandle = textureHandle;
    public StaticGeometryData Geometry = geometryData;
    public bool Released;

    public override string ToString() => $"TextureHandle={TextureHandle} Released={Released} Length={Geometry.Length}";
}
