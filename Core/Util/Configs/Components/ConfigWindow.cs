using Helion.Geometry;
using Helion.Util.Configs.Values;
using OpenTK.Windowing.Common;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components
{
    public class ConfigWindow
    {
        [ConfigInfo("The border of the window.")]
        public readonly ConfigValue<WindowBorder> Border = new(WindowBorder.Resizable, OnlyValidEnums<WindowBorder>());

        [ConfigInfo("The width and height of the window.")]
        public readonly ConfigValue<Dimension> Dimension = new((768, 1024), (_, dim) => dim.Area > 0);

        [ConfigInfo("The state of the window, such as if it is fullscreen or windowed.")]
        public readonly ConfigValue<WindowState> State = new(WindowState.Fullscreen, OnlyValidEnums<WindowState>());
    }
}
