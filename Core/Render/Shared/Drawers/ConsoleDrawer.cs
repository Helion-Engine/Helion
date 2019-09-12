using System.Drawing;
using Helion.Render.Commands;
using Helion.Render.Commands.Align;
using Helion.Render.Shared.Drawers.Helper;
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
        private const int ConsoleFontSize = 32;
        private const int BlackBarDividerHeight = 3;
        private const int CaretWidth = 2;
        private const int LeftEdgeOffset = 4;
        private const int InputToMessagePadding = 8;
        private const int BetweenMessagePadding = 3;
        private const long FlashSpanNanos = 500 * 1000L * 1000L;
        private const long HalfFlashSpanNanos = FlashSpanNanos / 2;
        private const float BackgroundAlpha = 0.95f;
        private const string ConsoleFontName = "Console";
        private static readonly Color BackgroundFade = Color.FromArgb(230, 0, 0, 0);
        private static readonly Color InputFlashColor = Color.FromArgb(0, 255, 0);
        
        public static void Draw(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            DrawHelper helper = new DrawHelper(renderCommands);
            
            renderCommands.ClearDepth();
            
            DrawBackgroundImage(viewport, helper);
            DrawInput(console, viewport, helper, out int inputDrawTop);
            DrawMessages(console, viewport, helper, inputDrawTop);
        }

        private static bool IsCursorFlashTime() => Ticker.NanoTime() % FlashSpanNanos < HalfFlashSpanNanos;

        private static void DrawBackgroundImage(Dimension viewport, DrawHelper helper)
        {
            Size size = new Size(1, 2);
            int middleY = viewport.Height / 2;

            // Draw the background, depending on what is available.
            if (helper.ImageExists("CONBACK"))
                helper.Image("CONBACK", 0, -middleY, viewport.Width, viewport.Height, BackgroundFade, BackgroundAlpha);
            else if (helper.ImageExists("TITLEPIC"))
                helper.Image("TITLEPIC", 0, -middleY, viewport.Width, viewport.Height, BackgroundFade, BackgroundAlpha);
            else
                helper.FillRect(0, middleY - BlackBarDividerHeight, viewport.Width, 3, Color.Gray);
            
            // Then draw the divider.
            helper.FillRect(0, middleY - BlackBarDividerHeight, viewport.Width, 3, Color.Black);
        }

        private static void DrawInput(HelionConsole console, Dimension viewport, DrawHelper helper, out int inputDrawTop)
        {
            int offsetX = LeftEdgeOffset;
            int middleY = viewport.Height / 2;
            int baseY = middleY - BlackBarDividerHeight - 5;

            helper.Text(Color.Yellow, console.Input, ConsoleFontName, ConsoleFontSize, offsetX, baseY, 
                        Alignment.BottomLeft, out Dimension drawArea);
            
            inputDrawTop = baseY - drawArea.Height;
            offsetX += drawArea.Width;
            
            if (IsCursorFlashTime())
            {
                // We want to pad right of the last character, only if there
                // are characters to draw.
                int cursorX = console.Input.Empty() ? offsetX : offsetX + 2;
                int barHeight = ConsoleFontSize - 2;
                helper.FillRect(cursorX, inputDrawTop, CaretWidth, barHeight, InputFlashColor);
            }
        }

        private static void DrawMessages(HelionConsole console, Dimension viewport, DrawHelper helper, int inputDrawTop)
        {
            int topY = inputDrawTop - InputToMessagePadding;

            foreach (ConsoleMessage msg in console.Messages)
            {
                helper.Text(msg.Message, ConsoleFontName, ConsoleFontSize, 4, topY, Alignment.BottomLeft, 
                            out Dimension drawArea);
                topY -= drawArea.Height + BetweenMessagePadding;

                if (topY < 0)
                    break;
            }
        }
    }
}