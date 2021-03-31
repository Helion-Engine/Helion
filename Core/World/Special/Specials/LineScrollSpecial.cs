using System.Linq;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;

namespace Helion.World.Special.Specials
{
    public class LineScrollSpecial : ISpecial
    {
        private readonly Line m_line;
        private readonly ZDoomLineScroll m_scroll;
        private readonly bool m_front;
        private double m_speedX;
        private double m_speedY;

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
                m_line.Front.ScrollData = new SideScrollData();
            else if (m_line.Back != null)
                m_line.Back.ScrollData = new SideScrollData();
        }

        public LineScrollSpecial(Line line, LineScrollSpecialModel model)
            : this (line, model.SpeedX, model.SpeedY, (ZDoomLineScroll)model.Scroll, model.Front)
        {
            if (m_front && m_line.Front.ScrollData != null && model.OffsetFrontX != null && model.OffsetFrontY != null)
            {
                m_line.Front.ScrollData.Offset = model.GenerateFrontOffsets();
                m_line.Front.ScrollData.LastOffset = model.GenerateFrontOffsets();
            }

            if (!m_front && m_line.Back?.ScrollData != null && model.OffsetBackX != null && model.OffsetBackY != null)
            {
                m_line.Back.ScrollData.Offset = model.GenerateBackOffsets();
                m_line.Back.ScrollData.LastOffset = model.GenerateBackOffsets();
            }
        }

        public ISpecialModel ToSpecialModel()
        {
            LineScrollSpecialModel model =  new LineScrollSpecialModel()
            {
                LineId = m_line.Id,
                Scroll = (int)m_scroll,
                SpeedX = m_speedX,
                SpeedY = m_speedY,
                Front = m_front
            };

            if (m_front && m_line.Front.ScrollData != null)
            {
                model.OffsetFrontX = m_line.Front.ScrollData.Offset.Select(v => v.X).ToArray();
                model.OffsetFrontY = m_line.Front.ScrollData.Offset.Select(v => v.Y).ToArray();
            }

            if (!m_front && m_line.Back?.ScrollData != null)
            {
                model.OffsetBackX = m_line.Back.ScrollData.Offset.Select(v => v.X).ToArray();
                model.OffsetBackY = m_line.Back.ScrollData.Offset.Select(v => v.Y).ToArray();
            }

            return model;
        }

        public SpecialTickStatus Tick()
        {
            if (m_front)
                Scroll(m_line.Front.ScrollData!);
            else if (m_line.Back != null)
                Scroll(m_line.Back.ScrollData!);

            return SpecialTickStatus.Continue;
        }

        public void ResetInterpolation()
        {
            double saveSpeedX = m_speedX;
            double saveSpeedY = m_speedY;
            m_speedX = m_speedY = 0;
            Tick();
            m_speedX = saveSpeedX;
            m_speedY = saveSpeedY;
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

            if (m_speedX != 0 || m_speedY != 0)
            {
                if (m_front)
                    m_line.Front.OffsetChanged = true;
                else if (m_line.Back != null)
                    m_line.Back.OffsetChanged = true;
            }
        }

        public void Use(Entity entity)
        {
            // Not used
        }
    }
}