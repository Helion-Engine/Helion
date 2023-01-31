using Helion.Maps.Specials;
using Helion.Maps.Specials.Vanilla;

namespace Helion.Resources.Definitions.MapInfo;

public class BossAction
{
    public BossAction(string actorName, VanillaLineSpecialType action, int tag)
    {
        ActorName = actorName;
        Action = action;
        Tag = tag;
    }

    public string ActorName { get; set; }
    public VanillaLineSpecialType Action { get; set; }
    public int Tag { get; set; }
}
