using Helion.World.Entities.Players;

namespace Helion.World;

public readonly struct PlayerMessageEvent(Player player, DisplayMessageArgs args)
{
    public readonly Player Player = player;
    public readonly DisplayMessageArgs Args = args;
}
