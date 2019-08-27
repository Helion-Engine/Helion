using System.Diagnostics;
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
        private static readonly Color InputColor = Color.FromArgb(0, 255, 0);
        
        public static void Draw(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            renderCommands.ClearDepth();
            
            DrawBackgroundImage(viewport, renderCommands);
            DrawInput(console, viewport, renderCommands);
            DrawMessages(console, viewport, renderCommands);
        }

        private static void DrawBackgroundImage(Dimension viewport, RenderCommands renderCommands)
        {
            renderCommands.DrawImage("TITLEPIC", 0, -viewport.Height / 2, viewport.Width, viewport.Height, BackgroundFade, 0.8f);
            
            // TODO: Implement DrawShape().
            renderCommands.DrawImage("TITLEPIC", 0, (viewport.Height / 2) - BlackBarDividerHeight, viewport.Width, 3, Color.Black);
        }

        private static void DrawInput(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            // TODO: Need some kind of drawing function or font maxHeight here.
            int baseY = (viewport.Height / 2) - BlackBarDividerHeight;
            
            // TODO: This should be done with better coloring.
            string input = console.Input;
            if (Ticker.NanoTime() % 500_000_000 < 250_000_000)
                input += "]";

            renderCommands.DrawText(input, "SmallFont", 4, baseY - 10, InputColor);
        }

        private static void DrawMessages(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            // TODO: Need some kind of drawing function or font maxHeight here.
            // along with the height from DrawInput().
            int topY = (viewport.Height / 2) - BlackBarDividerHeight - 25;

            foreach (ColoredString message in console.Messages)
            {
                renderCommands.DrawText(message.ToString(), "SmallFont", 4, topY);
                topY -= 10;

                if (topY < 0)
                    break;
            }
        }
    }
}