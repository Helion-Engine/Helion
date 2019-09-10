using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class LineScrollSpecial : ISpecial
    {
        public Sector? Sector => null;
        public Line? ActivatedLine;
        private Line m_line;
        private int m_speed;
        private ZDoomLineScroll m_scroll;

        public LineScrollSpecial(Line line, double speed, ZDoomLineScroll scroll)
        {
            m_line = line;
            m_speed = (int)speed;
            m_scroll = scroll;
        }

        public SpecialTickStatus Tick()
        {
            m_line.Front.Offset.X += m_speed;
            return SpecialTickStatus.Continue;
        }

        public void Use()
        {
        }
    }
}