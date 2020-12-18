using Helion.Resource.Textures;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Worlds.Textures.Types
{
    /// <summary>
    /// A texture that acts as a switch in a world.
    /// </summary>
    public class SwitchWorldTexture : IWorldTexture, ITickable
    {
        public CIString Name { get; }
        public Texture Texture { get; private set; }
        public bool IsMissing => false;
        public bool IsSky => false;
        private readonly Texture m_inactiveTexture;
        private readonly Texture m_activeTexture;
        private readonly int m_durationTicks;
        private int m_animatedTicksRemaining;

        public SwitchWorldTexture(CIString name, Texture inactiveTexture, Texture activeTexture, int duration)
        {
            Precondition(duration >= 0, "Cannot have a negative switch world texture duration");

            Name = name;
            Texture = inactiveTexture;
            m_inactiveTexture = inactiveTexture;
            m_activeTexture = activeTexture;
            m_durationTicks = duration;
        }

        public SwitchWorldTexture(SwitchWorldTexture parent)
        {
            Name = parent.Name;
            Texture = parent.Texture;
            m_inactiveTexture = parent.m_inactiveTexture;
            m_activeTexture = parent.m_activeTexture;
            m_durationTicks = parent.m_durationTicks;
            m_animatedTicksRemaining = parent.m_animatedTicksRemaining;
        }

        public void Activate()
        {
            Texture = m_activeTexture;
            m_animatedTicksRemaining = m_durationTicks;
        }

        public void Tick()
        {
            if (m_animatedTicksRemaining <= 0)
                return;

            m_animatedTicksRemaining--;
            if (m_animatedTicksRemaining == 0)
                Texture = m_inactiveTexture;
        }

        public override string ToString() => $"{Name} [remaining: {m_animatedTicksRemaining}]";
    }
}
