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

        [ConfigInfo("Emulate fake contrast like vanilla Doom.")]
        public readonly ConfigValueBoolean FakeContrast = new(true);

        [ConfigInfo("If any sprite should clip the floor.")]
        public readonly ConfigValueBoolean SpriteClip = new(true);

        [ConfigInfo("If corpse sprites should clip the floor.")]
        public readonly ConfigValueBoolean SpriteClipCorpse = new(true);

        [ConfigInfo("The minimum sprite height to allow to clip the floor.")]
        public readonly ConfigValueInt SpriteClipMin = new(16, 0);

        [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
        public readonly ConfigValueDouble SpriteClipFactorMax = new ConfigValueDouble(0.05, 0, 1);

        [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
        public readonly ConfigValueDouble SpriteClipCorpseFactorMax = new ConfigValueDouble(0.01, 0, 1);
    }
}
