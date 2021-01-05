using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with the in game HUD.")]
    public class ConfigHud
    {
        [ConfigInfo("If the full status bar should be drawn. If false, draws a minimalistic HUD.")]
        public readonly ConfigValueBoolean FullStatusBar = new(true);

        [ConfigInfo("The amount of move bobbing the weapon does. 0.0 is off, 1.0 is normal.")]
        public readonly ConfigValueDouble MoveBob = new(1.0);
    }
}
