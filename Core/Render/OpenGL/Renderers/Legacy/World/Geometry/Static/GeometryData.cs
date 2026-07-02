using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public sealed class GeometryData
{
    public int TextureHandle { get; set; }
    public GLLegacyTexture Texture { get; set; }
    public GLLegacyTexture? BrightmapTexture { get; set; }
    public VertexPipeline<StaticVertex> Pipeline { get; set; }

    public GeometryData(int textureHandle, GLLegacyTexture texture, VertexPipeline<StaticVertex> pipeline, GLLegacyTexture? brightmapTexture = null)
    {
        TextureHandle = textureHandle;
        Texture = texture;
        BrightmapTexture = brightmapTexture;
        Pipeline = pipeline;
    }

    public void Dispose()
    {
        Pipeline.Dispose();
    }
}
