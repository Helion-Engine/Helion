using System.Collections.Generic;
using Helion.Graphics;
using Helion.Resources.Textures.Animations;
using Helion.Resources.Textures.Sprites;
using Helion.Util.Configs;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Textures;

/// <summary>
/// A concrete implementation of a resource texture manager.
/// </summary>
public class ResourceTextureManager : IResourceTextureManager
{
    public const int NoTextureIndex = 0;
    private readonly ResourceTexture NullTexture;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IResources m_resources;
    private readonly ResourceTextureAnimationManager m_animationManager;
    private readonly ResourceSpriteManager m_resourceSpriteManager;
    private readonly ResourceTracker<ResourceTexture> m_textures = new();
    private readonly List<ResourceTexture> m_textureList = new();

    public int Count => m_textureList.Count;
    public IResourceTextureAnimationManager Animations => m_animationManager;
    public IResourceSpriteManager Sprites => m_resourceSpriteManager;

    public ResourceTextureManager(IResources resources, IConfig config)
    {
        m_resources = resources;
        m_animationManager = new ResourceTextureAnimationManager(resources, this);
        m_resourceSpriteManager = new ResourceSpriteManager(this);

        NullTexture = new(NoTextureIndex, "null", 
            config.Render.NullTexture ? Image.NullImage : Image.TransparentImage, ResourceNamespace.Global);
        m_textureList.Add(NullTexture);
    }

    public ResourceTexture this[int index] => GetByIndex(index);

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
                m_animationManager.Load(name, priorityNamespace);
                
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
                return m_textureList.Count >= 2 ? m_animationManager.Lookup(1) : NullTexture;
            default:
                return m_animationManager.Lookup(index);
        }
    }
}