using Helion.Resource.Definitions.Animations.Switches;
using Helion.Resource.Textures;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Worlds.Textures.Types
{
    // TODO: The implementation is incomplete, missing OFF, and animations if present.
    /// <summary>
    /// A texture that acts as a switch in a world.
    /// </summary>
    public class SwitchWorldTexture : IWorldTexture, ITickable
    {
        public CIString Name => m_animatedSwitch.Name;
        public Texture Texture { get; private set; }
        public bool IsMissing => false;
        public bool IsSky => false;
        private readonly AnimatedSwitch m_animatedSwitch;
        private readonly Texture m_inactiveTexture;
        private readonly Texture m_activeTexture;
        private readonly int m_durationTicks;
        private int m_animatedTicksRemaining;
        private bool m_stayActiveForever;

        public SwitchWorldTexture(AnimatedSwitch animatedSwitch, Texture inactiveTexture, Texture activeTexture,
            int duration)
        {
            Precondition(duration >= 0, "Cannot have a negative switch world texture duration");

            Texture = inactiveTexture;
            m_animatedSwitch = animatedSwitch;
            m_inactiveTexture = inactiveTexture;
            m_activeTexture = activeTexture;
            m_durationTicks = duration;
        }

        public SwitchWorldTexture(SwitchWorldTexture parent)
        {
            Texture = parent.Texture;
            m_animatedSwitch = parent.m_animatedSwitch;
            m_inactiveTexture = parent.m_inactiveTexture;
            m_activeTexture = parent.m_activeTexture;
            m_durationTicks = parent.m_durationTicks;
            m_animatedTicksRemaining = parent.m_animatedTicksRemaining;
        }

        /// <summary>
        /// Activates the switch.
        /// </summary>
        /// <param name="forever">True if this should be activated forever,
        /// meaning it never switches into the off position again, or false
        /// if it should use the internal timer and revert after that period
        /// of time.</param>
        public void Activate(bool forever)
        {
            Texture = m_activeTexture;
            m_animatedTicksRemaining = m_durationTicks;
            m_stayActiveForever = forever;
        }

        public void Tick()
        {
            if (m_stayActiveForever || m_animatedTicksRemaining <= 0)
                return;

            m_animatedTicksRemaining--;
            if (m_animatedTicksRemaining == 0)
                Texture = m_inactiveTexture;
        }

        public override string ToString() => $"{Name} [remaining: {m_animatedTicksRemaining}]";
    }
}
