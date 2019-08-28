using Helion.Maps.Geometry;

namespace Helion.Maps.Special.Specials
{
    public class LightStrobeSpecial : ISpecial
    {
        public Sector? Sector { get; }

        private short m_maxBright;
        private short m_minBright;
        private int m_brightTics;
        private int m_darkTics;
        private int m_delay;

        public LightStrobeSpecial(Sector sector, IRandom random, short minLightLevel, int brightTics, int darkTics, bool sync)
        {
            Sector = sector;
            m_brightTics = brightTics;
            m_darkTics = darkTics;
            m_maxBright = sector.LightLevel;
            m_minBright = minLightLevel;

            if (!sync)
                m_delay = random.NextRandom() & 7;
        }

        public SpecialTickStatus Tick(long gametic)
        {
            if (m_delay > 0)
            {
                m_delay--;
                return SpecialTickStatus.Continue;
            }

            if (Sector.LightLevel == m_maxBright)
            {
                Sector.SetLightLevel(m_minBright);
                m_delay = m_darkTics;            
            }
            else if (Sector.LightLevel == m_minBright)
            {
                Sector.SetLightLevel(m_maxBright);
                m_delay = m_brightTics;
            }

            return SpecialTickStatus.Continue;
        }

        public void Use()
        {
        }
    }
}
