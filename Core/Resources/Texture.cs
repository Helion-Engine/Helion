using Helion.Graphics;

namespace Helion.Resources;

public class Texture
{
    public Texture(string name, ResourceNamespace resourceNamespace, int index)
    {
        Name = name;
        Namespace = resourceNamespace;
        Index = index;
    }

    public void SetGLTexture(object glTexture, bool repeatY)
    {
        if (repeatY)
            RenderStore = glTexture;
        else
            RenderStoreClamp = glTexture;
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
    public object? RenderStoreClamp;
}
