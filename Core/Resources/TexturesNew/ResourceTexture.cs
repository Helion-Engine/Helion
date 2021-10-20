using Helion.Graphics;

namespace Helion.Resources.TexturesNew;

/// <summary>
/// A texture that derives itself from a resource. This is intended only for
/// data that comes from an archive, or some definition that is made up of
/// resources (like multiple patches).
/// </summary>
/// <param name="Index">The index of the texture, used in animations.</param>
/// <param name="Name">The name of the texture.</param>
/// <param name="Image">The image that makes up this texture.</param>
/// <param name="Namespace">The namespace of this texture.</param>
public record ResourceTexture(int Index, string Name, Image Image, ResourceNamespace Namespace);
