using Helion.Util.Configs.Values;
using Helion.World.StatusBar;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components
{
    public class ConfigHudAutoMap
    {
        [ConfigInfo("Amount to scale automap.", save: false)]
        public readonly ConfigValue<double> Scale = new(1.0, Clamp(0.1, 10.0));        
    }
    
    public class ConfigHud
    {
        public readonly ConfigHudAutoMap AutoMap = new();
        
        [ConfigInfo("The amount of move bobbing the weapon does. 0.0 is off, 1.0 is normal.")]
        public readonly ConfigValue<double> MoveBob = new(1.0, ClampNormalized);

        [ConfigInfo("Amount to scale minimal hud.")]
        public readonly ConfigValue<double> Scale = new(2.0, Greater(0.0));
        
        [ConfigInfo("The size of the status bar.")]
        public readonly ConfigValue<StatusBarSizeType> StatusBarSize = new(StatusBarSizeType.Minimal, OnlyValidEnums<StatusBarSizeType>());
    }
}
