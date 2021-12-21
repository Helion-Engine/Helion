using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigCompat
{
    [ConfigInfo("If dehacked should be preferred over decorate in the same archive.")]
    public readonly ConfigValue<bool> PreferDehacked = new(true);

    [ConfigInfo("Use vanilla sector physics. Floors can move through ceiling. Only one move special per sector at a time.", serialize: true)]
    public readonly ConfigValue<bool> VanillaSectorPhysics = new(false);

    [ConfigInfo("Vanilla method for finding shortest texture. Emulates bug with AASHITTY.", save: false, serialize: true)]
    public readonly ConfigValue<bool> VanillaShortestTexture = new(true);

    [ConfigInfo("Allow items to dropoff tall ledges.", save: false, serialize: true)]
    public readonly ConfigValue<bool> AllowItemDropoff = new(true);
}
