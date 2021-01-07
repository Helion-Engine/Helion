using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components.Renderer
{
    [ConfigInfo("Components for anisotropic filtering.")]
    public class ConfigRenderAnisotropy
    {
        [ConfigInfo("Whether anisotropic rendering should be used or not.")]
        public readonly ConfigValueBoolean Enable = new(true);

        [ConfigInfo("If true, overrides anisotropy to use the max value supported.")]
        public readonly ConfigValueBoolean UseMaxSupported = new(true);

        [ConfigInfo("The anisotropic filtering amount.")]
        public readonly ConfigValueDouble Value = new(8.0);
    }
}
