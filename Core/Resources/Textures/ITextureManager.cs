namespace Helion.Resources.Textures
{
    public interface ITextureManager
    {
        int EstimatedTextureCount { get; }
        
        bool TryGet(string name, ResourceNamespace priority, out Texture? texture);
    }
}
