using Helion.Util.Configs.Values;
using Helion.Window;

namespace Helion.Util.Configs.Components
{
    public class ConfigWindow
    {
        public readonly ConfigValueInt Height = new(768);
        public readonly ConfigValueEnum<WindowStatus> State = new(WindowStatus.Fullscreen);
        public readonly ConfigValueEnum<VerticalSync> VSync = new(VerticalSync.Off);
        public readonly ConfigValueInt Width = new(1024);
    }
}
