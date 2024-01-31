namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public readonly struct FloodGeometry
{
    public readonly int Key;
    public readonly int TextureHandle;
    public readonly int LightIndex;
    public readonly int VboOffset;
    public readonly int Vertices;

    public FloodGeometry(int key, int textureHandle, int lightIndex, int vboOffset, int vertices)
    {
        Key = key;
        TextureHandle = textureHandle;
        LightIndex = lightIndex;
        VboOffset = vboOffset;
        Vertices = vertices;
    }
}
