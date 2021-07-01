using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Timing;

namespace Helion.Layer.New.Consoles
{
    public partial class ConsoleLayerNew
    {
        private const int ConsoleFontSize = 32;
        private const int CaretWidth = 2;
        private const int LeftEdgeOffset = 8;
        private const int InputToMessagePadding = 8;
        private const int BetweenMessagePadding = 3;
        private const long FlashSpanNanos = 500 * 1000L * 1000L;
        private const long HalfFlashSpanNanos = FlashSpanNanos / 2;
        private const string ConsoleFontName = "Console";
        private static readonly Color BackgroundFade = Color.FromArgb(230, 0, 0, 0);
        private static readonly Color InputFlashColor = Color.FromArgb(0, 255, 0);
        
        private static bool IsCursorFlashTime => Ticker.NanoTime() % FlashSpanNanos < HalfFlashSpanNanos;
        
        public void Render(IHudRenderContext hud)
        {
            RenderBackground(hud);
            RenderInput(hud);
            RenderMessages(hud);
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

            Dimension drawArea = (hud.Width, hud.Height / 2);
            
            if (hud.ImageExists(ConsoleBackingImage))
                hud.Image(ConsoleBackingImage, (0, 0), drawArea, alpha: BackgroundAlpha);
            else if (hud.ImageExists(TitlepicImage))
                hud.Image(TitlepicImage, (0, 0), drawArea, alpha: BackgroundAlpha);
            else
                hud.FillBox(((0, 0), drawArea.Vector), Color.Gray);
        }

        private void RenderConsoleDivider(IHudRenderContext hud)
        {
            const int DividerHeight = 3;

            Box2I divider = ((0, 0), (hud.Width, DividerHeight));
            hud.FillBox(divider, Color.Black, Align.MiddleLeft);
        }

        private void RenderInput(IHudRenderContext hud)
        {
            // TODO
        }
        
        private void RenderMessages(IHudRenderContext hud)
        {
            // TODO
        }
    }
}
