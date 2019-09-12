using Helion.Render.Commands;
using Helion.Render.Shared;

namespace Helion.Render
{
    /// <summary>
    /// A renderer that can consume rendering commands and draw the results.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// A helper class that will calculate the draw area when trying to
        /// draw a string with some font and font size.
        /// </summary>
        IImageDrawInfoProvider ImageDrawInfoProvider { get; }
        
        /// <summary>
        /// Performs rendering of all the commands.
        /// </summary>
        /// <param name="renderCommands">A series of drawing commands.</param>
        void Render(RenderCommands renderCommands);
    }
}