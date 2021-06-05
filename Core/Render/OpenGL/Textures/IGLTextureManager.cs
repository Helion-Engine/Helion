using System;
using Helion.Resources;

namespace Helion.Render.OpenGL.Textures
{
    /// <summary>
    /// A manager of GL textures.
    /// </summary>
    public interface IGLTextureManager : IDisposable
    {
        /// <summary>
        /// The null texture handle. This is always returned from any texture
        /// query functions if they cannot be found.
        /// </summary>
        GLTextureHandle NullHandle { get; }
        
        /// <summary>
        /// The null font handle. This is always returned from any font query
        /// functions if they cannot be found.
        /// </summary>
        GLFontTextureHandle NullFontHandle { get; }

        /// <summary>
        /// Gets a texture with a name and priority namespace. If it cannot
        /// find one in the priority namespace, it will search others. If none
        /// can be found, the <see cref="NullHandle"/> is returned. If data is
        /// found for some existing texture in the resource texture manager, it
        /// will upload the texture data.
        /// </summary>
        /// <param name="name">The texture name, case insensitive.</param>
        /// <param name="priority">The first namespace to look at.</param>
        /// <returns>The texture handle, or the <see cref="NullHandle"/> if it
        /// cannot be found.</returns>
        GLTextureHandle Get(string name, ResourceNamespace priority);
        
        /// <summary>
        /// Looks up or creates a texture from an existing resource texture.
        /// </summary>
        /// <param name="texture">The texture to look up (or upload).</param>
        /// <returns>A texture handle.</returns>
        GLTextureHandle Get(Texture texture);
        
        /// <summary>
        /// Gets a font, or uploads it if it finds one and it has not been
        /// uploaded yet. If none can be found, the <see cref="NullFontHandle"/>
        /// is returned.
        /// </summary>
        /// <param name="name">The font name, case insensitive.</param>
        /// <returns>The font handle, or <see cref="NullFontHandle"/> if no font
        /// resource can be found.</returns>
        GLFontTextureHandle GetFont(string name);
    }
}
