using Helion.Render.Shared;
using Helion.Util.Configs.Components.Renderer;
using Helion.Util.Configs.Values;

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

        [ConfigInfo("If VSync should be on or off. Prevents tearing, but affects input processing (unless you have g-sync).")]
        public readonly ConfigValueBoolean VSync = new();
    }
}
