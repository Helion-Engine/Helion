using Helion.Render.Shared;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineRenderConfig
    {
        public readonly EngineRenderAnisotropyConfig Anisotropy = new EngineRenderAnisotropyConfig();
        public readonly ConfigValue<FilterType> FontFilter = new ConfigValue<FilterType>(FilterType.Trilinear);
        public readonly ConfigValue<double> FieldOfView = new ConfigValue<double>(90.0);
        public readonly EngineRenderMultisampleConfig Multisample = new EngineRenderMultisampleConfig();
        public readonly ConfigValue<FilterType> TextureFilter = new ConfigValue<FilterType>(FilterType.Trilinear);
        public readonly ConfigValue<bool> ShowFPS = new ConfigValue<bool>(false);
    }
}