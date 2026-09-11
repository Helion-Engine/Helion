namespace Helion.Render.OpenGL.Texture;

public interface IArrayTextureLookup
{
    // Returns unique base array texture handle given a texture handle from the TextureManager
    // If the texture does not have a parent array texture then the textureHandle parameter is returned
    int GetArrayTextureHandle(int textureHandle, bool repeatY);
}
