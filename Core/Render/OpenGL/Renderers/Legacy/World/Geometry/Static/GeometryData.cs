using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public sealed class GeometryData
{
    public GeometryData(int textureHandle, GLLegacyTexture texture, StaticVertexBuffer<LegacyVertex> vbo, VertexArrayObject vao)
    {
        TextureHandle = textureHandle;
        Texture = texture;
        Vbo = vbo;
        Vao = vao;
    }

    public int TextureHandle { get; set; }
    public GLLegacyTexture Texture { get; set; }
    public StaticVertexBuffer<LegacyVertex> Vbo { get; set; }
    public VertexArrayObject Vao { get; set; }
}
