using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineDeveloperConfig
    {
        public readonly ConfigValue<bool> GCStats = new ConfigValue<bool>(false);
        public readonly ConfigValue<bool> MouseFocus = new ConfigValue<bool>(true);
        public readonly ConfigValue<bool> RenderDebug = new ConfigValue<bool>(false);
        public readonly ConfigValue<bool> RemoveHitEntity = new ConfigValue<bool>(true);
        public readonly ConfigValue<bool> UseZdbsp = new ConfigValue<bool>(true);
    }
}