using Helion.Render.Shared;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineRenderConfig
    {
        public readonly EngineRenderAnisotropyConfig Anisotropy = new EngineRenderAnisotropyConfig();
        public readonly EngineRenderMultisampleConfig Multisample = new EngineRenderMultisampleConfig();
        public readonly ConfigValue<FilterType> Filter = new ConfigValue<FilterType>(FilterType.Trilinear);
        public readonly ConfigValue<double> FieldOfView = new ConfigValue<double>(90.0);
    }
}