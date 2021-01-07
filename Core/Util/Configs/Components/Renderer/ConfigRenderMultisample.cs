using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components.Renderer
{
    [ConfigInfo("Whether multisampling should be used or not.")]
    public class ConfigRenderMultisample
    {
        [ConfigInfo("If multisampling should be used.")]
        public readonly ConfigValueBoolean Enable = new();

        [ConfigInfo("The value of multisampling to use.")]
        public readonly ConfigValueInt Value = new(4);
    }
}
