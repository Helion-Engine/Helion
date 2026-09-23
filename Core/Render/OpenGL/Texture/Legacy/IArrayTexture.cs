using Helion.Render.Common.Textures;

namespace Helion.Render.OpenGL.Texture.Legacy;

public interface IArrayTexture
{
    string FetchTextureName { get; }
    IRenderableTextureHandle? Handle { get; set; }
    int ResolvedHeight { get; set; }
}
