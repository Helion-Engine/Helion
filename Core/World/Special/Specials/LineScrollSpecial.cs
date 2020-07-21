using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;

namespace Helion.World.Special.Specials
{
    public class LineScrollSpecial : ISpecial
    {
        private readonly Line m_line;
        private readonly double m_speedX;
        private readonly double m_speedY;
        private readonly ZDoomLineScroll m_scroll;
        private readonly bool m_front;

        public LineScrollSpecial(Line line, double speedX, double speedY, ZDoomLineScroll scroll, bool front = true)
        {
            m_line = line;
            m_speedX = speedX;
            m_speedY = speedY;

            if ((int)scroll > (int)ZDoomLineScroll.LowerTexture)
                m_scroll = ZDoomLineScroll.All;
            else
                m_scroll = scroll;

            m_front = front;
            if (m_front)
            {
                m_line.Front.ScrollData = new Maps.Specials.SideScrollData();
                m_line.Front.ScrollData.Offset[SideScrollData.MiddlePosition].X = 1000000;
            }
            else if (m_line.Back != null)
                m_line.Back.ScrollData = new Maps.Specials.SideScrollData();

            line.Front.Sector.DataChanged = true;
        }

        public SpecialTickStatus Tick()
        {
            if (m_front)
                Scroll(m_line.Front.ScrollData!);
            else if (m_line.Back != null)
                Scroll(m_line.Back.ScrollData!);

            return SpecialTickStatus.Continue;
        }

        private void Scroll(SideScrollData scrollData)
        {
            if (m_scroll == ZDoomLineScroll.All || (m_scroll & ZDoomLineScroll.UpperTexture) != 0)
            {
                scrollData.LastOffset[SideScrollData.UpperPosition] = scrollData.Offset[SideScrollData.UpperPosition];
                scrollData.Offset[SideScrollData.UpperPosition].X += m_speedX;
                scrollData.Offset[SideScrollData.UpperPosition].Y += m_speedY;
            }

            if (m_scroll == ZDoomLineScroll.All || (m_scroll & ZDoomLineScroll.MiddleTexture) != 0)
            {
                scrollData.LastOffset[SideScrollData.MiddlePosition] = scrollData.Offset[SideScrollData.MiddlePosition];
                scrollData.Offset[SideScrollData.MiddlePosition].X += m_speedX;
                scrollData.Offset[SideScrollData.MiddlePosition].Y += m_speedY;
            }

            if (m_scroll == ZDoomLineScroll.All || (m_scroll & ZDoomLineScroll.LowerTexture) != 0)
            {
                scrollData.LastOffset[SideScrollData.LowerPosition] = scrollData.Offset[SideScrollData.LowerPosition];
                scrollData.Offset[SideScrollData.LowerPosition].X += m_speedX;
                scrollData.Offset[SideScrollData.LowerPosition].Y += m_speedY;
            }
        }

        public void Use()
        {
        }
    }
}