using Helion.Util.Configs.Values;
using OpenTK.Windowing.Common;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with the game window.")]
    public class ConfigWindow
    {
        [ConfigInfo("The border of the window.")]
        public readonly ConfigValueEnum<WindowBorder> Border = new(WindowBorder.Fixed);

        [ConfigInfo("The height of the window.")]
        public readonly ConfigValueInt Height = new(768);

        [ConfigInfo("The state of the window, such as if it is fullscreen or windowed.")]
        public readonly ConfigValueEnum<WindowState> State = new(WindowState.Fullscreen);

        [ConfigInfo("The width of the window.")]
        public readonly ConfigValueInt Width = new(1024);
    }
}
