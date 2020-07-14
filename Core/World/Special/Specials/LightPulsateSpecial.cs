using Helion.Util;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class LightPulsateSpecial : ISectorSpecial
    {
        private const int DoomPulsateValue = 8;

        public Sector Sector { get; }
        private readonly short m_maxBright;
        private readonly short m_minBright;
        private int m_inc;

        public LightPulsateSpecial(Sector sector, short minLightLevel)
        {
            Sector = sector;
            m_maxBright = sector.LightLevel;
            m_minBright = minLightLevel;

            m_inc = -DoomPulsateValue;
        }

        public SpecialTickStatus Tick()
        {
            Sector.LightingChanged = true;
            int lightLevel = Sector.LightLevel + m_inc;
            lightLevel = MathHelper.Clamp(lightLevel, m_minBright, m_maxBright);
            Sector.SetLightLevel((short)lightLevel);

            if (Sector.LightLevel == m_minBright || Sector.LightLevel == m_maxBright)
                m_inc = -m_inc;

            return SpecialTickStatus.Continue;
        }

        public void FinalizeDestroy()
        {
        }

        public void Use()
        {
        }

        public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
    }
}
