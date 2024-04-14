using Helion.Maps.Specials;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;

namespace Helion.Resources.Definitions.MapInfo;

public class BossAction
{
    public BossAction(string actorName, VanillaLineSpecialType action, int tag)
    {
        ActorName = actorName;
        Action = action;
        Tag = tag;
    }

    public BossAction(string actorName, ZDoomLineSpecialType action, SpecialArgs args)
    {
        ActorName = actorName;
        ZDoomAction = action;
        ZDoomSpecialArgs = args;
    }

    public string ActorName { get; set; }
    public VanillaLineSpecialType? Action { get; set; }
    public ZDoomLineSpecialType? ZDoomAction { get; set; }
    public SpecialArgs ZDoomSpecialArgs { get; set; }
    public int Tag { get; set; }
}
