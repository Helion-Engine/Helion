using System.Collections.Generic;
using Helion.Geometry;

namespace Helion.Resources.Definitions.Texture;

/// <summary>
/// A universal and shared definition for an image that is to be compiled
/// out of one or more images.
/// </summary>
public class TextureDefinition
{
    /// <summary>
    /// The name of this image.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// The dimensions of the image.
    /// </summary>
    public readonly Dimension Dimension;

    /// <summary>
    /// The components (sub-images) that make up the image.
    /// </summary>
    public readonly IList<TextureDefinitionComponent> Components;

    /// <summary>
    /// The namespace of the image.
    /// </summary>
    public readonly ResourceNamespace Namespace;

    /// <summary>
    /// The width of the image.
    /// </summary>
    public int Width => Dimension.Width;

    /// <summary>
    /// The height of the image.
    /// </summary>
    public int Height => Dimension.Height;

    public readonly TextureOptions Options;

    /// <summary>
    /// Creates a new texture definition.
    /// </summary>
    /// <param name="name">The name of the texture.</param>
    /// <param name="dimension">The dimensions of the image.</param>
    /// <param name="resourceNamespace">The namespace for this definition.
    /// </param>
    /// <param name="components">A list of all the sub-images that make up
    /// this definition.</param>
    /// <param name="options">The options for this texture.</param>
    public TextureDefinition(string name, Dimension dimension, ResourceNamespace resourceNamespace,
        IList<TextureDefinitionComponent> components, TextureOptions? options = null)
    {
        Name = name;
        Dimension = dimension;
        Components = components;
        Namespace = resourceNamespace;
        Options = options ?? TextureOptions.Default;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Dimension}, {Namespace})";
}
