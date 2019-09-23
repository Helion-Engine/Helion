using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class LightFireFlickerDoom : ISectorSpecial
    {
        public Sector Sector { get; }
        private readonly IRandom m_random;
        private readonly short m_minBright;
        private readonly short m_maxBright;
        private int m_delay;

        public LightFireFlickerDoom(Sector sector, IRandom random, short minLightLevel)
        {
            Sector = sector;
            m_random = random;
            m_minBright = (short)(minLightLevel + 16);
            m_maxBright = Sector.LightLevel;
        }

        public SpecialTickStatus Tick()
        {
            Sector.LightingChanged = true;

            if (m_delay > 0)
            {
                m_delay--;
                return SpecialTickStatus.Continue;
            }

            int change = (m_random.NextByte() & 3) * 16;

            if (Sector.LightLevel - change < m_minBright)
                Sector.SetLightLevel(m_minBright);
            else
                Sector.SetLightLevel((short)(m_maxBright - change));

            m_delay = 4;

            return SpecialTickStatus.Continue;
        }

        public void FinalizeDestroy()
        {
        }

        public void Use()
        {
        }
    }
}