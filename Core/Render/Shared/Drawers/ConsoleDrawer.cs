using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Time;

namespace Helion.Render.Shared.Drawers
{
    /// <summary>
    /// Performs console drawing by issuing rendering commands.
    /// </summary>
    public static class ConsoleDrawer
    {
        private const int BlackBarDividerHeight = 3;
        private const int LeftEdgeOffset = 4;
        private const int InputToMessagePadding = 8;
        private const int BetweenMessagePadding = 3;
        private static readonly Color BackgroundFade = Color.FromArgb(230, 0, 0, 0);
        private static readonly Color InputFlashColor = Color.FromArgb(0, 255, 0);
        
        public static void Draw(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            renderCommands.ClearDepth();
            
            DrawBackgroundImage(viewport, renderCommands);
            DrawInput(console, viewport, renderCommands, out int inputDrawTop);
            DrawMessages(console, viewport, renderCommands, inputDrawTop);
        }

        private static bool IsCursorFlashTime() => Ticker.NanoTime() % 500_000_000 < 250_000_000;

        private static void DrawBackgroundImage(Dimension viewport, RenderCommands renderCommands)
        {
            int middleY = viewport.Height / 2;
            
            renderCommands.DrawImage("TITLEPIC", 0, -middleY, viewport.Width, viewport.Height, BackgroundFade, 0.8f);
            renderCommands.FillRect(0, middleY - BlackBarDividerHeight, viewport.Width, 3, Color.Black);
        }

        private static void DrawInput(HelionConsole console, Dimension viewport, RenderCommands renderCommands,
            out int inputDrawTop)
        {
            int fontHeight = 8; // TODO: Actually get the font height!
            int middleY = viewport.Height / 2;
            int baseY = middleY - BlackBarDividerHeight - 5;
            ColoredString str = ColoredStringBuilder.From(Color.Yellow, console.Input);

            renderCommands.DrawText(str, "SmallFont", LeftEdgeOffset, baseY - fontHeight, out Rectangle drawArea);
            inputDrawTop = drawArea.Top;

            if (IsCursorFlashTime())
            {
                // We want to pad right of the last character, only if there
                // are characters to draw.
                int left = console.Input.Empty() ? drawArea.Right : drawArea.Right + 2;
                
                Rectangle drawRect = new Rectangle(left, drawArea.Top, 2, drawArea.Height);
                renderCommands.FillRect(drawRect, InputFlashColor);
            }
        }

        private static void DrawMessages(HelionConsole console, Dimension viewport, RenderCommands renderCommands,
            int inputDrawTop)
        {
            int fontHeight = 8; // TODO: Actually get the font height!
            int topY = inputDrawTop - InputToMessagePadding - fontHeight;

            foreach (ColoredString message in console.Messages)
            {
                renderCommands.DrawText(message, "SmallFont", 4, topY);
                topY -= fontHeight + BetweenMessagePadding;

                if (topY < 0)
                    break;
            }
        }
    }
}