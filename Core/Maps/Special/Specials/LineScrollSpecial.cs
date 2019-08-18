using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;

namespace Helion.Maps.Special.Specials
{
    public class LineScrollSpecial : ISpecial
    {
        public Sector? Sector => null;
        public Line? ActivatedLine { get; set; }

        private Line m_line;
        private int m_speed;
        private ZLineScroll m_scroll;

        public LineScrollSpecial(Line line, double speed, ZLineScroll scroll)
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
