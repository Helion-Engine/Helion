using Helion.Render.Commands;

namespace Helion.Render
{
    /// <summary>
    /// A renderer that can consume rendering commands and draw the results.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Performs rendering of all the commands.
        /// </summary>
        /// <param name="renderCommands">A series of drawing commands.</param>
        void Render(RenderCommands renderCommands);
    }
}