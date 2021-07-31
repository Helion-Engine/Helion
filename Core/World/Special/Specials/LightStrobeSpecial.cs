using Helion.Models;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class LightStrobeSpecial : SectorSpecialBase
    {
        private readonly short m_maxBright;
        private readonly short m_minBright;
        private readonly int m_brightTics;
        private readonly int m_darkTics;
        private int m_delay;
        
        public LightStrobeSpecial(Sector sector, IRandom random, short minLightLevel, int brightTics, int darkTics, bool sync)
        {
            Sector = sector;
            m_brightTics = brightTics;
            m_darkTics = darkTics;
            m_maxBright = sector.LightLevel;
            m_minBright = minLightLevel;

            if (m_minBright == m_maxBright)
                m_minBright = 0;

            if (!sync)
                m_delay = random.NextByte() & 0x07;
        }

        public LightStrobeSpecial(Sector sector, LightStrobeSpecialModel model)
        {
            Sector = sector;
            m_brightTics = model.BrightTics;
            m_darkTics = model.DarkTics;
            m_maxBright = model.Max;
            m_minBright = model.Min;
            m_delay = model.Delay;
        }

        public override ISpecialModel? ToSpecialModel()
        {
            return new LightStrobeSpecialModel()
            {
                SectorId = Sector.Id,
                Max = m_maxBright,
                Min = m_minBright,
                BrightTics = m_brightTics,
                DarkTics = m_darkTics,
                Delay = m_delay
            };
        }

        public override SpecialTickStatus Tick()
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

        public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
    }
}
