using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components.Renderer
{
    [ConfigInfo("Components for anisotropic filtering.")]
    public class ConfigRenderAnisotropy
    {
        [ConfigInfo("Whether anisotropic rendering should be used or not.")]
        public readonly ConfigValueBoolean Enable = new(true);

        [ConfigInfo("If true, uses the maximum supported value by the system. This takes priority over the value.")]
        public readonly ConfigValueBoolean UseMaxSupported = new(true);

        [ConfigInfo("The anisotropic filtering amount.")]
        public readonly ConfigValueDouble Value = new(8.0);
    }
}
