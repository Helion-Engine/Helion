using Helion.Util.Configs.Values;
using Helion.World.StatusBar;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with the in game HUD.")]
    public class ConfigHud
    {
        [ConfigInfo("The size of the status bar.")]
        public readonly ConfigValueEnum<StatusBarSizeType> StatusBarSize = new(StatusBarSizeType.Minimal);

        [ConfigInfo("The amount of move bobbing the weapon does. 0.0 is off, 1.0 is normal.")]
        public readonly ConfigValueDouble MoveBob = new(1.0);

        [ConfigInfo("Amount to scale minimal hud.")]
        public readonly ConfigValueDouble Scale = new ConfigValueDouble(1.0, 0.0);
    }
}
