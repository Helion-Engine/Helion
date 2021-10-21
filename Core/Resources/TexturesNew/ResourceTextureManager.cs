using System.Collections.Generic;
using Helion.Graphics;
using Helion.Resources.TexturesNew.Animations;
using NLog;
using static Helion.Util.Assertion.Assert;

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
        return TryGetInternal(name, priorityNamespace, out texture, true);
    }
    
    // This does the job of TryGet, but it can be called by the animation
    // manager without fear of infinite recursion.
    internal bool TryGetInternal(string name, ResourceNamespace priorityNamespace, out ResourceTexture texture,
        bool alsoLoadFromAnimationManager)
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

            // This must come after the texture is loaded above, so it can be
            // found, and not go into an infinite loop.
            if (alsoLoadFromAnimationManager)
                m_animations.Load(name, priorityNamespace);
                
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
        
        return texture;
    }

    public ResourceTexture GetNullCompatibilityTexture(int index)
    {
        switch (index)
        {
            case < 0:
                Fail("Trying to get null compatibility texture with a negative index");
                return NullTexture;
            case NoTextureIndex:
                return m_textureList.Count >= 2 ? m_animations.Lookup(1) : NullTexture;
            default:
                return m_animations.Lookup(index);
        }
    }
}