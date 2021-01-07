using Helion.Util.Configs.Values;
using Helion.Window;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with the game window.")]
    public class ConfigWindow
    {
        [ConfigInfo("The height of the window.")]
        public readonly ConfigValueInt Height = new(768);

        [ConfigInfo("The state of the window, such as if it is fullscreen or windowed.")]
        public readonly ConfigValueEnum<WindowStatus> State = new(WindowStatus.Fullscreen);

        [ConfigInfo("The width of the window.")]
        public readonly ConfigValueInt Width = new(1024);
    }
}
