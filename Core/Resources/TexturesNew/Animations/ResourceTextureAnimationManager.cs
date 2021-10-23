using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Util.Extensions;

namespace Helion.Resources.TexturesNew.Animations;

/// <summary>
/// A concrete implementation of a texture animation manager.
/// </summary>
public class ResourceTextureAnimationManager : IResourceTextureAnimationManager
{
    private readonly IResources m_resources;
    private readonly ResourceTextureManager m_textureManager;
    private readonly List<ResourceAnimation> m_animations = new();
    private readonly Dictionary<int, ResourceAnimation> m_textureIndexToAnimation = new();

    public ResourceTextureAnimationManager(IResources resources, ResourceTextureManager textureManager)
    {
        m_resources = resources;
        m_textureManager = textureManager;
    }
    
    public ResourceTexture Lookup(int textureIndex)
    {
        if (m_textureIndexToAnimation.TryGetValue(textureIndex, out ResourceAnimation? animation))
            return animation.Texture;
        return ResourceTextureManager.NullTexture;
    }

    internal void Load(string name, ResourceNamespace priorityNamespace)
    {
        // TODO: Looking this up instead of iterating over a list would be much better.
        List<AnimatedTexture> animTextures = m_resources.Animdefs.AnimatedTextures;
        
        foreach (AnimatedTexture t in animTextures.Where(t => t.Name.EqualsIgnoreCase(name)))
            LoadAnimation(name, t, priorityNamespace);
    }

    private void LoadAnimation(string name, AnimatedTexture animatedTexture, ResourceNamespace priorityNamespace)
    {
        List<ResourceAnimationTexture> animationTextures = new();
        List<int> textureIndicesForThisAnimation = new();
        
        foreach (AnimatedTextureComponent component in animatedTexture.Components)
        {
            // Remember that this can return the "null missing texture", so even
            // if it fails, that is okay, we just want some texture. Since we are
            // calling into who is directly calling us, we must avoid recursion too.
            m_textureManager.TryGetInternal(name, priorityNamespace, out ResourceTexture texture, false);
            
            // TODO: Implement this properly with oscillating, random, etc, one day.
            ResourceAnimationTexture animationTexture = new(texture, component.MaxTicks);
            animationTextures.Add(animationTexture);
            
            textureIndicesForThisAnimation.Add(texture.Index);
        }
        
        ResourceAnimation animation = new(animationTextures);
        m_animations.Add(animation);

        foreach (int textureIndex in textureIndicesForThisAnimation)
            m_textureIndexToAnimation[textureIndex] = animation;
    }

    public void Tick()
    {
        // Don't generate any GC pressure since this will get called every tick.
        for (int i = 0; i < m_animations.Count; i++)
            m_animations[i].Tick();
    }
}