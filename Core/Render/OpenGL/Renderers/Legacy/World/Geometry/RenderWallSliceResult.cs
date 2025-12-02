using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Texture.Legacy;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public ref struct RenderWallSliceResult(Span<DynamicVertex> vertices, SkyGeometryVertex[]? skyVertices, GLLegacyTexture? texture, SkyGeometryVertex[]? skyVertices2 = null, bool addOffset = true)
{
    public Span<DynamicVertex> Vertices = vertices;
    public SkyGeometryVertex[]? SkyVertices = skyVertices;
    public SkyGeometryVertex[]? SkyVertices2 = skyVertices2;
    public GLLegacyTexture? Texture = texture;
    public bool AddOffset = addOffset;

    public static RenderWallSliceResult EmptyMiddle => new([], null, null, addOffset: false);
}
