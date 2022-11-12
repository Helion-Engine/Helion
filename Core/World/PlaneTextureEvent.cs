using Helion.World.Geometry.Sectors;

namespace Helion.World;

public readonly struct PlaneTextureEvent
{
    public readonly SectorPlane Plane;
    public readonly int TextureHandle;
    public readonly int PreviousTextureHandle;

    public PlaneTextureEvent(SectorPlane plane, int textureHandle, int previousTextureHandle)
    {
        Plane = plane;
        TextureHandle = textureHandle;
        PreviousTextureHandle = previousTextureHandle;
    }
}
