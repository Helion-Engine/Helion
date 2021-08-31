using Helion.Geometry;
using Helion.Render.Common.Textures;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components
{
    public class ConfigRenderAnisotropy
    {
        [ConfigInfo("Whether anisotropic rendering should be used or not.")]
        public readonly ConfigValue<bool> Enable = new(true);

        [ConfigInfo("If true, overrides anisotropy to use the max value supported.")]
        public readonly ConfigValue<bool> UseMaxSupported = new(false);

        [ConfigInfo("The anisotropic filtering amount.")]
        public readonly ConfigValue<int> Value = new(8, GreaterOrEqual(1));
    }
    
    public class ConfigRenderMultisample
    {
        [ConfigInfo("Whether multisampling should be used or not.")]
        public readonly ConfigValue<bool> Enable = new(true);

        [ConfigInfo("If true, overrides multisampling to use the max value supported.")]
        public readonly ConfigValue<bool> UseMaxSupported = new(false);

        [ConfigInfo("The multisampling amount.")]
        public readonly ConfigValue<int> Value = new(8, GreaterOrEqual(1));
    }

    public class ConfigRenderFilter
    {
        [ConfigInfo("The kind of filter applied to fonts.")]
        public readonly ConfigValue<FilterType> Font = new(FilterType.Nearest, OnlyValidEnums<FilterType>());
        
        [ConfigInfo("The filter to be applied to textures.")]
        public readonly ConfigValue<FilterType> Texture = new(FilterType.Nearest, OnlyValidEnums<FilterType>());
    }

    public class ConfigRenderVirtualDimension
    {
        [ConfigInfo("Whether virtual dimensions should be used or not.")]
        public readonly ConfigValue<bool> Enable = new(true);
        
        [ConfigInfo("The width and height of the virtual dimension.")]
        public readonly ConfigValue<Dimension> Dimension = new((640, 480), (_, dim) => dim.Area > 0);
    }
    
    public class ConfigRender
    {
        public readonly ConfigRenderAnisotropy Anisotropy = new();
        
        [ConfigInfo("Emulate fake contrast like vanilla Doom.")]
        public readonly ConfigValue<bool> FakeContrast = new(true);

        public readonly ConfigRenderFilter Filter = new();

        [ConfigInfo("Emulate light dropoff like vanilla Doom.")]
        public readonly ConfigValue<bool> LightDropoff = new(true);
        
        [ConfigInfo("A cap on the maximum amount of frames per second. Zero is equivalent to no cap.")]
        public readonly ConfigValue<int> MaxFPS = new(0);

        public readonly ConfigRenderMultisample Multisample = new();

        [ConfigInfo("If the frames per second should be rendered.")]
        public readonly ConfigValue<bool> ShowFPS = new(false);

        [ConfigInfo("If any sprite should clip the floor.")]
        public readonly ConfigValue<bool> SpriteClip = new(true);

        [ConfigInfo("If corpse sprites should clip the floor.")]
        public readonly ConfigValue<bool> SpriteClipCorpse = new(true);

        [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
        public readonly ConfigValue<double> SpriteClipCorpseFactorMax = new(0.01, ClampNormalized);

        [ConfigInfo("Max percentage of height allowed to clip the floor for corpses.")]
        public readonly ConfigValue<double> SpriteClipFactorMax = new(0.05, ClampNormalized);

        [ConfigInfo("The minimum sprite height to allow to clip the floor.")]
        public readonly ConfigValue<int> SpriteClipMin = new(16, GreaterOrEqual(0));

        public readonly ConfigRenderVirtualDimension VirtualDimension = new();
        
        [ConfigInfo("If VSync should be on or off. Prevents tearing, but affects input processing (unless you have g-sync).")]
        public readonly ConfigValue<bool> VSync = new(false);
    }
}
