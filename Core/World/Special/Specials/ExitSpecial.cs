using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class ExitSpecial : ISpecial
    {
        public Sector? Sector => null;

        private int m_delay;

        public ExitSpecial(int delayTics)
        {
            m_delay = delayTics;
        }

        public SpecialTickStatus Tick()
        {
            if (m_delay > 0)
            {
                m_delay--;
                return SpecialTickStatus.Continue;
            }

            return SpecialTickStatus.Destroy;
        }

        public void Use()
        {
        }
    }
}