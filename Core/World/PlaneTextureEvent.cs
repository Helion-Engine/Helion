using Helion.World.Geometry.Sectors;

namespace Helion.World;

public readonly struct PlaneTextureEvent
{
    public readonly SectorPlane Plane;
    public readonly int TextureHandle;

    public PlaneTextureEvent(SectorPlane plane, int textureHandle)
    {
        Plane = plane;
        TextureHandle = textureHandle;
    }
}
