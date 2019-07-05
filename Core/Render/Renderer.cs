using Helion.Render.Commands;

namespace Helion.Render
{
    public interface IRenderer
    {
        void Render(RenderCommands renderCommands);
    }
}