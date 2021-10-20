using System.Collections.Generic;
using Helion.Graphics;
using NLog;

namespace Helion.Resources.TexturesNew;

/// <summary>
/// Responsible for managing textures that come from a resource collection.
/// </summary>
public class ResourceTextureManager
{
    public const int NoTextureIndex = 0;
    public static readonly ResourceTexture NullTexture = new(NoTextureIndex, "null", Image.NullImage, ResourceNamespace.Global);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly ResourceTextureAnimations Animations;
    private readonly IResources m_resources;
    private readonly ResourceTracker<ResourceTexture> m_textures = new();
    private readonly List<ResourceTexture> m_textureList = new() { NullTexture };

    public ResourceTextureManager(IResources resources)
    {
        m_resources = resources;
        Animations = new ResourceTextureAnimations(resources, this);
    }

    /// <summary>
    /// Looks up the texture, or creates one if it has not been found yet.
    /// </summary>
    /// <param name="name">The texture name.</param>
    /// <param name="priorityNamespace">The first namespace to look in.</param>
    /// <param name="texture">Always returns a texture. If the function returns
    /// true, then a texture was created. It may not be from the same namespace
    /// though. If it returns false, that means it could not find, nor make the
    /// texture, and the null texture was returned (which is not a null reference).
    /// </param>
    /// <returns>True if found (or made), false if unable to find.</returns>
    public bool TryGet(string name, ResourceNamespace priorityNamespace, out ResourceTexture texture)
    {
        texture = NullTexture;

        ResourceTexture? trackedTexture = m_textures.Get(name, priorityNamespace);
        if (trackedTexture != null)
        {
            if (ReferenceEquals(texture, NullTexture))
                return false;
            
            // If we found something from another namespace, insert that so we
            // don't have to keep doing multiple lookups in the resource tracker.
            if (trackedTexture.Namespace != priorityNamespace)
                m_textures.Insert(name, priorityNamespace, trackedTexture);
            
            texture = trackedTexture;
            return true;
        }
        
        Image? image = m_resources.ImageRetriever.Get(name, priorityNamespace);
        if (image != null)
        {
            texture = CreateNewTexture(name, priorityNamespace, image);
            return true;
        }

        // If it cannot be made, then place the dummy in it's place so lookups
        // early-out if called again.
        m_textures.Insert(name, priorityNamespace, NullTexture);
        return false;
    }

    /// <summary>
    /// Gets a texture by index. If the index is out of range, then the
    /// <see cref="NullTexture"/> is returned.
    /// </summary>
    /// <param name="index">The index to get.</param>
    /// <returns>The texture, or the "null texture" singleton.</returns>
    public ResourceTexture GetByIndex(int index)
    {
        return index >= 0 && index < m_textureList.Count ? m_textureList[index] : NullTexture;
    }

    private ResourceTexture CreateNewTexture(string name, ResourceNamespace priorityNamespace, Image image)
    {
        Log.Trace("Creating new texture {0} ({1})", name, priorityNamespace);

        int index = m_textureList.Count;
        ResourceTexture texture = new(index, name, image, priorityNamespace);
        m_textures.Insert(name, priorityNamespace, texture);
        m_textureList.Add(texture);
        
        return texture;
    }

    /// <summary>
    /// Get a texture by index, checks if the index is null and returns Doom's
    /// zero indexed texture (usually AASHITTY). This function is only intended
    /// to be used for vanilla compatibility like FloorRaiseByTexture.
    /// </summary>
    /// <param name="index">The index of the texture.</param>
    /// <returns>Returns a texture at the given index. If the texture is
    /// animated it's current animation texture will be returned.</returns>
    public ResourceTexture GetNullCompatibilityTexture(int index)
    {
        // TODO
        return NullTexture;
    }
}