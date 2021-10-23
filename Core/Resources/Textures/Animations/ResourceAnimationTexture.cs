using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Textures.Animations;

/// <summary>
/// An animation is composed of one or more of these.
/// </summary>
public class ResourceAnimationTexture
{
    public readonly ResourceTexture Texture;
    public readonly int Ticks;
    public int TicksLeft { get; private set; }

    public ResourceAnimationTexture(ResourceTexture texture, int ticks)
    {
        Precondition(ticks >= 0, "Must have a positive animation duration");
        
        Texture = texture;
        Ticks = ticks;
        TicksLeft = ticks;
    }

    /// <summary>
    /// Ticks the animation. If this reaches zero, that means the caller must
    /// advance the animation to the next texture. This also resets it so it
    /// is ready to be ticked again.
    /// </summary>
    /// <returns>True if the animation duration is over, false if there is more
    /// time for ticking in the future before the texture index in the owner
    /// needs to be advanced.</returns>
    public bool TickAndCheckIfShouldAdvance()
    {
        TicksLeft--;

        if (TicksLeft >= 0) 
            return false;
        
        TicksLeft = Ticks;
        return true;
    }
}