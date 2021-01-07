using Helion.Render.Shared;
using Helion.Util.Configs.Components.Renderer;
using Helion.Util.Configs.Values;
using Helion.Window;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with rendering.")]
    public class ConfigRender
    {
        public readonly ConfigRenderAnisotropy Anisotropy = new();

        [ConfigInfo("The kind of filter applied to fonts.")]
        public readonly ConfigValueEnum<FilterType> FontFilter = new(FilterType.Nearest);

        [ConfigInfo("A cap on the maximum amount of frames per second. Zero is equivalent to no cap.")]
        public readonly ConfigValueInt MaxFPS = new();

        public readonly ConfigRenderMultisample Multisample = new();

        [ConfigInfo("If the frames per second should be rendered.")]
        public readonly ConfigValueBoolean ShowFPS = new();

        [ConfigInfo("The filter to be applied to textures.")]
        public readonly ConfigValueEnum<FilterType> TextureFilter = new(FilterType.Nearest);

        [ConfigInfo("If VSync should be on or off.",
            "There are multiple types. If you want very accurate mouse movement, this should be off. However, it will cause tearing. An adaptive mode tries to get the best of both worlds (if supported).")]
        public readonly ConfigValueEnum<VerticalSync> VSync = new(VerticalSync.Off);
    }
}
