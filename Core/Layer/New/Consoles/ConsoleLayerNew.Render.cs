using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Timing;

namespace Helion.Layer.New.Consoles
{
    public partial class ConsoleLayerNew
    {
        private const long FlashSpanNanos = 500 * 1000L * 1000L;
        private const long HalfFlashSpanNanos = FlashSpanNanos / 2;
        private const string FontName = "Console";
        private const int FontSize = 32;
        
        private const int LeftEdgeOffset = 8;
        private const int InputToMessagePadding = 8;
        private const int BetweenMessagePadding = 3;
        private static readonly Color BackgroundFade = Color.FromArgb(230, 0, 0, 0);
        private static readonly Color InputFlashColor = Color.FromArgb(0, 255, 0);
        
        private static bool IsCursorFlashTime => Ticker.NanoTime() % FlashSpanNanos < HalfFlashSpanNanos;
        
        public void Render(IHudRenderContext hud)
        {
            RenderBackground(hud);
            RenderInput(hud, out int inputHeight);
            RenderMessages(hud, inputHeight);
        }

        private void RenderBackground(IHudRenderContext hud)
        {
            RenderConsoleBackground(hud);
            RenderConsoleDivider(hud);
        }

        private void RenderConsoleBackground(IHudRenderContext hud)
        {
            const string ConsoleBackingImage = "CONBACK";
            const string TitlepicImage = "TITLEPIC";
            const float BackgroundAlpha = 0.95f;

            HudBox drawArea = (0, 0, hud.Width, hud.Height / 2);
            
            if (hud.ImageExists(ConsoleBackingImage))
                hud.Image(ConsoleBackingImage, drawArea, alpha: BackgroundAlpha);
            else if (hud.ImageExists(TitlepicImage))
                hud.Image(TitlepicImage, drawArea, alpha: BackgroundAlpha);
            else
                hud.FillBox(drawArea, Color.Gray);
        }

        private void RenderConsoleDivider(IHudRenderContext hud)
        {
            const int DividerHeight = 3;

            HudBox dividerArea = (0, 0, hud.Width, DividerHeight);
            hud.FillBox(dividerArea, Color.Black, Align.MiddleLeft);
        }

        private void RenderInput(IHudRenderContext hud, out int inputHeight)
        {
            hud.Text(m_console.Input, FontName, FontSize, (4, -4), out Dimension drawArea, window: Align.MiddleLeft, anchor: Align.BottomLeft);
            RenderInputCursorFlash(hud, drawArea);

            inputHeight = drawArea.Height;
        }

        private void RenderInputCursorFlash(IHudRenderContext hud, Dimension drawArea)
        {
            const int BarHeight = FontSize - 2;
            const int CaretWidth = 2;

            if (!IsCursorFlashTime)
                return;
            
            Vec2I origin = (m_console.Input == "" ? 4 : 6, 0);
            Vec2I dimension = (CaretWidth, BarHeight);
            HudBox area = (origin, origin + dimension);
            hud.FillBox(area, InputFlashColor, Align.MiddleLeft, Align.BottomLeft);
        }

        private void RenderMessages(IHudRenderContext hud, int inputHeight)
        {
            // TODO
        }
    }
}
