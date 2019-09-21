using Helion.Util;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class LightChangeSpecial : ISectorSpecial
    {
        public Sector Sector { get; }
        private short m_lightLevel;
        private int m_step;
        private int m_min;
        private int m_max;

        public LightChangeSpecial(Sector sector, short lightLevel, int fadeTics)
        {
            Sector = sector;
            m_lightLevel = lightLevel;

            if (fadeTics > 0)
                m_step = (m_lightLevel - Sector.LightLevel) / fadeTics;
            else
                m_step = m_lightLevel - Sector.LightLevel;

            if (m_step < 0)
            {
                m_min = m_lightLevel;
                m_max = sector.LightLevel;
            }
            else
            {
                m_min = sector.LightLevel;
                m_max = m_lightLevel;
            }
        }

        public SpecialTickStatus Tick()
        {
            Sector.ActiveLightSpecial = this;
            int set = MathHelper.Clamp(Sector.LightLevel + m_step, m_min, m_max);
            Sector.SetLightLevel((short)set);

            if (set == m_lightLevel)
                return SpecialTickStatus.Destroy;

            return SpecialTickStatus.Continue;
        }

        public void FinalizeDestroy()
        {
            Sector.ActiveLightSpecial = null;
        }

        public void Use()
        {
        }
    }
}