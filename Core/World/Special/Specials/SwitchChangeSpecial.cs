using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Switches;

namespace Helion.World.Special.Specials
{
    public class SwitchChangeSpecial : ISpecial
    {
        public Sector? Sector => null;
        private SwitchManager m_manager;
        private Line m_line;
        private bool m_repeat;
        private int m_switchDelayTics;

        public SwitchChangeSpecial(SwitchManager manager, Line line)
        {
            m_manager = manager;
            m_line = line;
            m_repeat = line.Flags.Repeat;
            m_line.Activated = true;
        }

        public SpecialTickStatus Tick()
        {
            if (m_switchDelayTics > 0)
            {
                m_switchDelayTics--;
                return SpecialTickStatus.Continue;
            }

            m_manager.SetLineSwitch(m_line);

            if (m_repeat)
            {
                m_switchDelayTics = 35;
                m_repeat = false;
                return SpecialTickStatus.Continue;
            }

            if (m_line.Flags.Repeat)
                m_line.Activated = false;

            return SpecialTickStatus.Destroy;
        }

        public void Use()
        {
        }
    }
}