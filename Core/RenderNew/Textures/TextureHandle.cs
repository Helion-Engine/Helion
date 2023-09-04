using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Resources;

namespace Helion.RenderNew.Textures;

/// <summary>
/// A handle into an atlas.
/// </summary>
/// <param name="Id">A sequential unique index. This can be used in shaders to
/// look up</param>
/// <param name="Name">The name of the image</param>
/// <param name="Dimension">The texture dimension.</param>
/// <param name="Position">The atlas positions in the atlas texture.</param>
/// <param name="UV">The UV coordinate bounds in the atlas texture.</param>
/// <param name="Namespace">The resource namespace.</param>
public record TextureHandle(int Id, string Name, Box2I Position, Box2F UV, ResourceNamespace Namespace)
{
    public const int NullId = 0;

    public Dimension Dimension => Position.Dimension;
    public bool IsNullTexture => Id == NullId;
}
