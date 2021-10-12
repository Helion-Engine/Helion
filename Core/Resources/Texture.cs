using Helion.Graphics;
using Helion.Util;

namespace Helion.Resources;

public class Texture
{
    public Texture(string name, ResourceNamespace resourceNamespace, int index)
    {
        Name = name;
        Namespace = resourceNamespace;
        Index = index;
    }

    /// <summary>
    /// Name of the texture.
    /// </summary>
    public string Name;

    /// <summary>
    /// Resource namespace of the texture.
    /// </summary>
    public ResourceNamespace Namespace;

    /// <summary>
    /// Cached image of the texture.
    /// </summary>
    public Image? Image;

    /// <summary>
    /// Index of the texture.
    /// </summary>
    public int Index;

    /// <summary>
    /// Cached rendering object of the texture.
    /// </summary>
    public object? RenderStore;
}
