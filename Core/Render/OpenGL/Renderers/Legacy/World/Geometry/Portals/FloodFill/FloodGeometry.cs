namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public readonly struct FloodGeometry(int key, int textureHandle, int lightIndex, int colorMapIndex, int vboOffset, int vertices)
{
    public readonly int Key = key;
    public readonly int TextureHandle = textureHandle;
    public readonly int LightIndex = lightIndex;
    public readonly int ColorMapIndex = colorMapIndex;
    public readonly int VboOffset = vboOffset;
    public readonly int Vertices = vertices;
}
