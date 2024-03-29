using Helion.Geometry.Vectors;

namespace Helion.Resources.Definitions.Texture;

/// <summary>
/// A component of a texture definition, which is a sub-image in the image
/// to be compiled.
/// </summary>
public class TextureDefinitionComponent
{
    /// <summary>
    /// The name of the component.
    /// </summary>
    public string Name;

    /// <summary>
    /// The offset relative to the top left of the main texture definition.
    /// </summary>
    public Vec2I Offset;

    public readonly TextureComponentOptions Options;

    /// <summary>
    /// Creates a new texture definition component.
    /// </summary>
    /// <param name="name">The name of the component.</param>
    /// <param name="offset">The offsets relative to the top left of the
    /// texture definition image.</param>
    /// <param name="options">The options for the component.</param>
    public TextureDefinitionComponent(string name, Vec2I offset, TextureComponentOptions? options = null)
    {
        Name = name;
        Offset = offset;
        Options = options ?? TextureComponentOptions.Default;
    }
}
