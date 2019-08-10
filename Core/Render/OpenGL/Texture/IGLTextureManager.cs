using System;
using Helion.Resources;
using Helion.Util;

namespace Helion.Render.OpenGL.Texture
{
    /// <summary>
    /// Provides texture retrieval of loaded resources.
    /// </summary>
    public interface IGLTextureManager : IDisposable
    {
        /// <summary>
        /// The null texture, intended to be used when the actual texture
        /// cannot be found.
        /// </summary>
        GLTexture NullTexture { get; }

        /// <summary>
        /// Gets the texture, with priority given to the namespace provided. If
        /// it cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="priorityNamespace">The namespace to search first.
        /// </param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        GLTexture Get(CIString name, ResourceNamespace priorityNamespace);
        
        /// <summary>
        /// Gets the texture, with priority given to the texture namespace. If
        /// it cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        GLTexture GetWall(CIString name);
        
        /// <summary>
        /// Gets the texture, with priority given to the flat namespace. If it
        /// cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The flat texture name.</param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        GLTexture GetFlat(CIString name);
    }
}