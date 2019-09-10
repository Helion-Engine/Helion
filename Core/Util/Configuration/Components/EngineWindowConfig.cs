using Helion.Util.Configuration.Attributes;
using Helion.Window;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineWindowConfig
    {
        public readonly ConfigValue<int> Height = new ConfigValue<int>(768);
        public readonly ConfigValue<WindowStatus> State = new ConfigValue<WindowStatus>(WindowStatus.Fullscreen);
        public readonly ConfigValue<VerticalSync> VSync = new ConfigValue<VerticalSync>(VerticalSync.Off);
        public readonly ConfigValue<int> Width = new ConfigValue<int>(1024);
    }
}