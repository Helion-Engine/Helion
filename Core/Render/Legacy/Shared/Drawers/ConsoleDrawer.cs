using System.Drawing;
using Helion.Geometry;
using Helion.Render.Legacy.Commands;
using Helion.Render.Legacy.Commands.Alignment;
using Helion.Render.Legacy.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Legacy.Shared.Drawers
{
    /// <summary>
    /// Performs console drawing by issuing rendering commands.
    /// </summary>
    public class ConsoleDrawer
    {
        private const int ConsoleFontSize = 32;
        private const int BlackBarDividerHeight = 3;
        private const int CaretWidth = 2;
        private const int LeftEdgeOffset = 8;
        private const int InputToMessagePadding = 8;
        private const int BetweenMessagePadding = 3;
        private const long FlashSpanNanos = 500 * 1000L * 1000L;
        private const long HalfFlashSpanNanos = FlashSpanNanos / 2;
        private const float BackgroundAlpha = 0.95f;
        private const string ConsoleFontName = "Console";
        private static readonly Color BackgroundFade = Color.FromArgb(230, 0, 0, 0);
        private static readonly Color InputFlashColor = Color.FromArgb(0, 255, 0);

        private readonly ArchiveCollection m_archiveCollection;

        public ConsoleDrawer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }

        public void Draw(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            DrawHelper helper = new(renderCommands);
            Font consoleFont = m_archiveCollection.Data.TrueTypeFontsDeprecated[ConsoleFontName];

            renderCommands.ClearDepth();

            DrawBackgroundImage(viewport, helper);
            DrawInput(console, viewport, helper, consoleFont, out int inputDrawTop);
            DrawMessages(console, viewport, helper, inputDrawTop, consoleFont);
        }

        private static bool IsCursorFlashTime() => Ticker.NanoTime() % FlashSpanNanos < HalfFlashSpanNanos;

        private void DrawBackgroundImage(Dimension viewport, DrawHelper draw)
        {
            (int width, int height) = viewport;
            int halfHeight = viewport.Height / 2;

            // Draw the background, depending on what is available.
            if (draw.ImageExists("CONBACK"))
                draw.Image("CONBACK", 0, 0, width, height, color: BackgroundFade, alpha: BackgroundAlpha);
            else if (draw.ImageExists("TITLEPIC"))
                draw.Image("TITLEPIC", 0, -halfHeight, width, height, color: BackgroundFade, alpha: BackgroundAlpha);
            else
                draw.FillRect(0, 0, width, 3, Color.Gray);

            // Draw the divider.
            draw.FillRect(0, halfHeight - BlackBarDividerHeight, viewport.Width, 3, Color.Black);
        }

        private void DrawInput(HelionConsole console, Dimension viewport, DrawHelper draw, Font consoleFont,
            out int inputDrawTop)
        {
            int offsetX = LeftEdgeOffset;
            int maxWidth = viewport.Width - offsetX;
            int middleY = viewport.Height / 2;
            int baseY = middleY - BlackBarDividerHeight - 5;

            draw.Text(Color.Yellow, console.Input, consoleFont, ConsoleFontSize, out Dimension drawArea,
                offsetX, baseY, textbox: Align.BottomLeft, maxWidth: maxWidth);

            inputDrawTop = baseY - ConsoleFontSize;
            offsetX += drawArea.Width;

            if (IsCursorFlashTime())
            {
                // We want to pad right of the last character, only if there
                // are characters to draw.
                int cursorX = console.Input.Empty() ? offsetX : offsetX + 2;
                int barHeight = ConsoleFontSize - 2;
                draw.FillRect(cursorX, inputDrawTop, CaretWidth, barHeight, InputFlashColor);
            }
        }

        private void DrawMessages(HelionConsole console, Dimension viewport, DrawHelper draw, int inputDrawTop,
            Font consoleFont)
        {
            int topY = inputDrawTop - InputToMessagePadding;
            int maxWidth = viewport.Width - LeftEdgeOffset;

            foreach (ConsoleMessage msg in console.Messages)
            {
                draw.Text(msg.Message, consoleFont, ConsoleFontSize, out Dimension drawArea,
                    LeftEdgeOffset, topY, textbox: Align.BottomLeft, maxWidth: maxWidth);

                topY -= drawArea.Height + BetweenMessagePadding;
                if (topY < 0)
                    break;
            }
        }
    }
}