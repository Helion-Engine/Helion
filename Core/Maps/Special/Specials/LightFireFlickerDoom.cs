using Helion.Maps.Geometry;

namespace Helion.Maps.Special.Specials
{
    public class LightFireFlickerDoom : ISpecial
    {
        public Sector Sector { get; private set; }

        private readonly IRandom m_random;
        private int m_delay;
        private short m_minBright;
        private short m_maxBright;

        public LightFireFlickerDoom(Sector sector, IRandom random, short minLightLevel)
        {
            Sector = sector;
            m_random = random;
            m_minBright = (short)(minLightLevel + 16);
            m_maxBright = Sector.LightLevel;
        }

        public SpecialTickStatus Tick(long gametic)
        {
            if (m_delay > 0)
            {
                m_delay--;
                return SpecialTickStatus.Continue;
            }

            int change = (m_random.NextRandom() & 3) * 16;

            if (Sector.LightLevel - change < m_minBright)
                Sector.SetLightLevel(m_minBright);
            else
                Sector.SetLightLevel((short)(m_maxBright - change));

            m_delay = 4;

            return SpecialTickStatus.Continue;
        }

        public void Use()
        {
        }
    }
}
