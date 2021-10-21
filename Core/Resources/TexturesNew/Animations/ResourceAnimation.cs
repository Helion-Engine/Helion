using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.TexturesNew.Animations;

/// <summary>
/// An animation is meant to be a collection of textures that are cycled through
/// every tick. To get the current texture to use for this animation, one only
/// needs to call <see cref="Texture"/> and it will return the up-to-date texture.
/// </summary>
public class ResourceAnimation : ITickable
{
    private readonly List<ResourceAnimationTexture> m_animationTextures;
    private int m_currentAnimationTextureIndex;

    public ResourceTexture Texture => AnimationTexture.Texture;
    private ResourceAnimationTexture AnimationTexture => m_animationTextures[m_currentAnimationTextureIndex];

    public ResourceAnimation(List<ResourceAnimationTexture> animationTextures)
    {
        Precondition(!animationTextures.Empty(), "Cannot create an empty animation");
        
        m_animationTextures = animationTextures;
    }

    public void Tick()
    {
        // If we have iterated through all of the ticks for the current animation
        // texture, then we need to move our pointer ahead to the next animation
        // texture. This function handles the state, so we don't need to touch the
        // internals of the AnimationTexture.
        if (AnimationTexture.TickAndCheckIfShouldAdvance())
        {
            m_currentAnimationTextureIndex++;
            m_currentAnimationTextureIndex %= m_animationTextures.Count;
        }
    }
}