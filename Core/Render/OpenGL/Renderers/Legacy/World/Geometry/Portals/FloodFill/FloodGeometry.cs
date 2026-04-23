namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public readonly struct FloodGeometry(int key, int textureHandle, int overrideLightIndex, float renderOptions, int vboOffset, int vertices)
{
    public readonly int Key = key;
    public readonly int TextureHandle = textureHandle;
    public readonly int OverrideLightIndex = overrideLightIndex;
    public readonly float RenderOptions = renderOptions;
    public readonly int VboOffset = vboOffset;
    public readonly int Vertices = vertices;
}
