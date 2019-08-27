using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Util;
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
        private static readonly Color BackgroundFade = Color.FromArgb(230, 0, 0, 0);
        private static readonly Color InputFlashColor = Color.FromArgb(0, 255, 0);
        
        public static void Draw(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            renderCommands.ClearDepth();
            
            DrawBackgroundImage(viewport, renderCommands);
            DrawInput(console, viewport, renderCommands, out int drawHeight);
            DrawMessages(console, viewport, renderCommands, drawHeight);
        }

        private static bool IsCursorFlashTime() => Ticker.NanoTime() % 500_000_000 < 250_000_000;

        private static void DrawBackgroundImage(Dimension viewport, RenderCommands renderCommands)
        {
            renderCommands.DrawImage("TITLEPIC", 0, -viewport.Height / 2, viewport.Width, viewport.Height, BackgroundFade, 0.8f);
            renderCommands.FillRect(0, (viewport.Height / 2) - BlackBarDividerHeight, viewport.Width, 3, Color.Black);
        }

        private static void DrawInput(HelionConsole console, Dimension viewport, RenderCommands renderCommands,
            out int drawHeight)
        {
            ColoredString str = ColoredStringBuilder.From(Color.Yellow, console.Input);
            int baseY = (viewport.Height / 2) - BlackBarDividerHeight;
            
            renderCommands.DrawText(str, "SmallFont", 4, baseY - 10, out Rectangle drawArea);
            drawHeight = drawArea.Height;

            if (IsCursorFlashTime())
            {
                Rectangle drawRect = new Rectangle(drawArea.Right + 2, drawArea.Top, 4, drawArea.Height);
                renderCommands.FillRect(drawRect, InputFlashColor);
            }
        }

        private static void DrawMessages(HelionConsole console, Dimension viewport, RenderCommands renderCommands,
            int drawHeight)
        {
            int topY = (viewport.Height / 2) - BlackBarDividerHeight - drawHeight - 15;

            foreach (ColoredString message in console.Messages)
            {
                // TODO: We should remove the out value and not calculate it, waste of computation!
                renderCommands.DrawText(message, "SmallFont", 4, topY, out _);
                topY -= 10;

                if (topY < 0)
                    break;
            }
        }
    }
}