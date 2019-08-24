using System.Drawing;
using Helion.Render.Commands;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Render.Shared.Drawers
{
    /// <summary>
    /// Performs console drawing by issuing rendering commands.
    /// </summary>
    public static class ConsoleDrawer
    {
        private static readonly Color BackgroundFade = Color.FromArgb(230, 0, 0, 0);
        
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
            renderCommands.DrawImage("TITLEPIC", 0, (viewport.Height / 2) - 3, viewport.Width, 3, Color.Black);
        }

        private static void DrawInput(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            // TODO
        }

        private static void DrawMessages(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            // TODO
        }
    }
}