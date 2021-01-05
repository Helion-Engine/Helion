using Helion.Input;
using Helion.Render;
using Helion.Util.Configs;
using Helion.Util.Geometry;

namespace Helion.Window
{
    public interface IWindow
    {
        int WindowID { get; }
        IRenderer Renderer { get; }
        Dimension WindowDimension { get; }
        InputEvent PollInput(Config config);
    }
}
