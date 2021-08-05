using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with compatibility.")]
    public class ConfigCompat
    {
        [ConfigInfo("If dehacked should be preferred over decorate in the same archive.")]
        public readonly ConfigValueBoolean PreferDehacked = new(true);

        [ConfigInfo("Use vanilla sector physics. Floors can move through ceiling. Only one move special per sector at a time.")]
        public readonly ConfigValueBoolean VanillaSectorPhysics = new(false);
    }
}
