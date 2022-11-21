using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Buffer.Array.Vertex;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Render.Legacy.Vertex;
using Helion.Render.Renderers.World;
using Helion.Render.Renderers.World.Geometry.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Render.Renderers.World.Geometry.Static;

public sealed class GeometryData
{
    public GeometryData(int textureHandle, GLLegacyTexture texture, StaticVertexBuffer<WorldVertex> vbo, VertexArrayObject vao)
    {
        TextureHandle = textureHandle;
        Texture = texture;
        Vbo = vbo;
        Vao = vao;
    }

    public int TextureHandle { get; set; }
    public GLLegacyTexture Texture { get; set; }
    public StaticVertexBuffer<WorldVertex> Vbo { get; set; }
    public VertexArrayObject Vao { get; set; }
}
