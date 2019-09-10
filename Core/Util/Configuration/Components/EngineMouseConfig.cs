using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineMouseConfig
    {
        public readonly ConfigValue<double> Pitch = new ConfigValue<double>(1.0);
        public readonly ConfigValue<double> PixelDivisor = new ConfigValue<double>(1024.0);
        public readonly ConfigValue<double> Sensitivity = new ConfigValue<double>(1.0);
        public readonly ConfigValue<double> Yaw = new ConfigValue<double>(1.0);
        public readonly ConfigValue<bool> MouseLook = new ConfigValue<bool>(true);
    }
}