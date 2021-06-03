using System.Drawing;
using SystemColor = System.Drawing.Color;

namespace Helion.Render.Legacy.Commands.Types
{
    public record ClearRenderCommand : IRenderCommand
    {
        public static readonly Color DefaultClearColor = SystemColor.Black;

        public readonly bool Color;
        public readonly bool Depth;
        public readonly bool Stencil;
        public readonly Color ClearColor;

        public ClearRenderCommand(bool color, bool depth, bool stencil, Color clearColor)
        {
            Color = color;
            Depth = depth;
            Stencil = stencil;
            ClearColor = clearColor;
        }

        public static ClearRenderCommand All() => All(SystemColor.FromArgb(16, 16, 16));

        public static ClearRenderCommand All(Color clearColor)
        {
            return new ClearRenderCommand(true, true, true, clearColor);
        }

        public static ClearRenderCommand DepthOnly()
        {
            return new ClearRenderCommand(false, true, false, DefaultClearColor);
        }
    }
}