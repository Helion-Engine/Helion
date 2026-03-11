using Helion.Util.Container;
namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

internal sealed class FreeGeometryList
{
    public DynamicArray<FreeGeometryData> Geometry = new(32);
    public int LastReleasedIndex = -1;
}
