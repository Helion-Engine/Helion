using Helion.Render.Shared;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineRenderConfig
    {
        public readonly EngineRenderAnisotropyConfig Anisotropy = new EngineRenderAnisotropyConfig();
        public readonly ConfigValue<FilterType> FontFilter = new ConfigValue<FilterType>(FilterType.Trilinear);
        public readonly EngineRenderMultisampleConfig Multisample = new EngineRenderMultisampleConfig();
        public readonly ConfigValue<FilterType> TextureFilter = new ConfigValue<FilterType>(FilterType.Trilinear);
        public readonly ConfigValue<bool> ShowFPS = new ConfigValue<bool>(false);
        public readonly ConfigValue<int> MaxFPS = new ConfigValue<int>(0);
    }
}