using System.Collections.Generic;
using Helion.Graphics;
using Helion.Resources.TexturesNew.Animations;
using NLog;

namespace Helion.Resources.TexturesNew;

/// <summary>
/// A concrete implementation of an <see cref="IResourceTextureManager"/>.
/// </summary>
public class ResourceTextureManager : IResourceTextureManager
{
    public const int NoTextureIndex = 0;
    public static readonly ResourceTexture NullTexture = new(NoTextureIndex, "null", Image.NullImage, ResourceNamespace.Global);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IResources m_resources;
    private readonly ResourceTextureAnimations m_animations;
    private readonly ResourceTracker<ResourceTexture> m_textures = new();
    private readonly List<ResourceTexture> m_textureList = new() { NullTexture };

    public IResourceTextureAnimations Animations => m_animations;

    public ResourceTextureManager(IResources resources)
    {
        m_resources = resources;
        m_animations = new ResourceTextureAnimations(resources, this);
    }

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
        m_animations.Add(texture);
        
        return texture;
    }

    public ResourceTexture GetNullCompatibilityTexture(int index)
    {
        // TODO
        return NullTexture;
    }
}