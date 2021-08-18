namespace Helion.Resources.Textures
{
    public interface ITextureManager
    {
        int CalculateTotalTextureCount();
        
        bool TryGet(string name, ResourceNamespace priority, out Texture? texture);
    }
}
