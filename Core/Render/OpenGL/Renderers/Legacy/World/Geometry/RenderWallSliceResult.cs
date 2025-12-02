using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Texture.Legacy;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public ref struct RenderWallSliceResult
{
    public Span<DynamicVertex> Vertices;
    public SkyGeometryVertex[]? SkyVertices;
    public SkyGeometryVertex[]? SkyVertices2;
    public GLLegacyTexture? Texture;
    public bool AddOffset;

    public RenderWallSliceResult(DynamicVertex[]? vertices, SkyGeometryVertex[]? skyVertices, GLLegacyTexture? texture, SkyGeometryVertex[]? skyVertices2 = null, bool addOffset = true)
    {
        Vertices = vertices == null ? [] : vertices.AsSpan();
        SkyVertices = skyVertices;
        Texture = texture;
        SkyVertices2 = skyVertices2;
        AddOffset = addOffset;
    }

    public static RenderWallSliceResult EmptyMiddle => new([], null, null, addOffset: false);
}
