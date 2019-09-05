using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class LightFlickerDoomSpecial : ISpecial
    {
        public Sector Sector { get; private set; }

        private readonly IRandom m_random;
        private short m_maxBright;
        private short m_minBright;
        private int m_delay;

        public LightFlickerDoomSpecial(Sector sector, IRandom random, short minLightLevel)
        {
            Sector = sector;
            m_random = random;
            m_maxBright = sector.LightLevel;
            m_minBright = minLightLevel;
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
                m_delay = (m_random.NextByte() & 7) + 1;
            }
            else
            {
                Sector.SetLightLevel(m_maxBright);
                m_delay = (m_random.NextByte() & 31) + 1;
            }

            return SpecialTickStatus.Continue;
        }

        public void Use()
        {
        }
    }
}