using Helion.World.Entities.Players;

namespace Helion.World;

public readonly struct PlayerMessageEvent
{
    public readonly Player Player;
    public readonly string Message;

    public PlayerMessageEvent(Player player, string message)
    {
        Player = player;
        Message = message;
    }
}
