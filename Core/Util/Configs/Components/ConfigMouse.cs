using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with mouse movement in menus and in game.")]
    public class ConfigMouse
    {
        [ConfigInfo("If the mouse should be focused on the window or not.")]
        public readonly ConfigValueBoolean Focus = new(true);

        [ConfigInfo("If we should be able to look around the level with the mouse.")]
        public readonly ConfigValueBoolean Look = new(true);

        [ConfigInfo("The vertical sensitivity. This is multiplied by the sensitivity value as well.")]
        public readonly ConfigValueDouble Pitch = new(1.0);

        [ConfigInfo("A scaling factor that allows other sensitivities to be reasonable values.",
            "A divisor that smooths our motion. This is primarily intended to make it so pitch/yaw/sensitivity can stay as reasonable values, instead of having to make them something like 0.0001. In short, for each pixel we move, that number is divided by this value. It then is translated into how many radians we rotate. As this is increased, the less we move (since it is a divisor and the result gets smaller).",
            advanced: true)]
        public readonly ConfigValueDouble PixelDivisor = new(1024.0);

        [ConfigInfo("If the mouse should use raw input.")]
        public readonly ConfigValueBoolean RawInput = new(true);

        [ConfigInfo("A scale for both the pitch and yaw, meaning this affects both axes.")]
        public readonly ConfigValueDouble Sensitivity = new(1.0);

        [ConfigInfo("The horizontal sensitivity. This is multiplied by the sensitivity value as well.")]
        public readonly ConfigValueDouble Yaw = new(1.0);
    }
}
