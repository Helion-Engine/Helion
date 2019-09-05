using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class DelayedExecuteSpecial : ISpecial
    {
        public Sector? Sector => null;

        private readonly SpecialManager m_specialManager;
        private readonly ISpecial m_special;
        private int m_delay;

        public DelayedExecuteSpecial(SpecialManager specialManager, ISpecial special, int delay)
        {
            m_specialManager = specialManager;
            m_special = special;
            m_delay = delay;
        }

        public SpecialTickStatus Tick(long gametic)
        {
            if (m_delay > 0)
            {
                m_delay--;
                return SpecialTickStatus.Continue;
            }

            m_specialManager.AddSpecial(m_special);

            return SpecialTickStatus.Destroy;
        }

        public void Use()
        {
        }
    }
}
