namespace Helion.Resources.Textures;

public interface ITextureManager
{
    bool TryGet(string name, ResourceNamespace priority, out Texture? texture);
}

