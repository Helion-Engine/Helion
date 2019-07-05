using Helion.Input;
using Helion.Render;
using Helion.Util.Geometry;

namespace Helion.Window
{
    public interface IWindow
    {
        int GetId();
        InputEvent PollInput();
        IRenderer GetRenderer();
        Dimension GetDimension();
    }
}
