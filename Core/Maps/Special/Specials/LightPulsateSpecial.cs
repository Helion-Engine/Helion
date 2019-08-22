using Helion.Maps.Geometry;
using Helion.Util;

namespace Helion.Maps.Special.Specials
{
    public class LightPulsateSpecial : ISpecial
    {
        public Sector? Sector { get; private set; }

        private const int DoomPulsateValue = 8;

        private byte m_maxBright;
        private byte m_minBright;
        private int m_inc;

        public LightPulsateSpecial(Sector sector, byte minLightLevel)
        {
            Sector = sector;
            m_maxBright = sector.LightLevel;
            m_minBright = minLightLevel;

            m_inc = -DoomPulsateValue;
        }

        public SpecialTickStatus Tick(long gametic)
        {
            int lightLevel = Sector.LightLevel + m_inc;
            lightLevel = MathHelper.Clamp(lightLevel, m_minBright, m_maxBright);
            Sector.SetLightLevel((byte)lightLevel);

            if (Sector.LightLevel == m_minBright || Sector.LightLevel == m_maxBright)
                m_inc = -m_inc;

            return SpecialTickStatus.Continue;
        }

        public void Use()
        {
        }
    }
}
