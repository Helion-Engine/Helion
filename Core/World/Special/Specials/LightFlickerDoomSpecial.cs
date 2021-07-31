using Helion.Models;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class LightFlickerDoomSpecial : SectorSpecialBase
    {
        private readonly IRandom m_random;
        private readonly short m_maxBright;
        private readonly short m_minBright;
        private int m_delay;

        public LightFlickerDoomSpecial(Sector sector, IRandom random, short minLightLevel)
        {
            Sector = sector;
            m_random = random;
            m_maxBright = sector.LightLevel;
            m_minBright = minLightLevel;
        }

        public LightFlickerDoomSpecial(Sector sector, IRandom random, LightFlickerDoomSpecialModel model)
        {
            Sector = sector;
            m_random = random;
            m_maxBright = model.Max;
            m_minBright = model.Min;
            m_delay = model.Delay;
        }

        public override ISpecialModel? ToSpecialModel()
        {
            return new LightFlickerDoomSpecialModel()
            {
                SectorId = Sector.Id,
                Max = m_maxBright,
                Min = m_minBright,
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
                m_delay = (m_random.NextByte() & 7) + 1;
            }
            else
            {
                Sector.SetLightLevel(m_maxBright);
                m_delay = (m_random.NextByte() & 31) + 1;
            }

            return SpecialTickStatus.Continue;
        }

        public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Light;
    }
}