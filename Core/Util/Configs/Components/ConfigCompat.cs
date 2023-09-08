using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigCompat
{
    [ConfigInfo("Vanilla method for finding shortest texture. Emulates bug with AASHITTY.", save: false, serialize: true, demo: true)]
    public readonly ConfigValue<bool> VanillaShortestTexture = new(true);

    [ConfigInfo("If dehacked should be preferred over decorate in the same archive.", demo: true)]
    public readonly ConfigValue<bool> PreferDehacked = new(true);

    [ConfigInfo("Use vanilla sector physics. Floors can move through ceiling. Only one move special per sector at a time.", serialize: true, demo: true)]
    public readonly ConfigValue<bool> VanillaSectorPhysics = new(false);

    [ConfigInfo("Allow items to dropoff tall ledges.", serialize: true, demo: true)]
    public readonly ConfigValue<bool> AllowItemDropoff = new(true);

    [ConfigInfo("Emulate vanilla infinitely tall things.", serialize: true, demo: true)]
    public readonly ConfigValue<bool> InfinitelyTallThings = new(false);

    [ConfigInfo("Things use their original vanilla heights for projectile collision checks.", serialize: true, demo: true)]
    public readonly ConfigValue<bool> MissileClip = new(false);

    [ConfigInfo("Limits lost souls spawn by pain elementals to 21.", serialize: true, demo: true)]
    public readonly ConfigValue<bool> PainElementalLostSoulLimit = new(false);
}
