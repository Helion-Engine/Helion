using Helion.World.Entities.Players;

namespace Helion.World;

public readonly struct PlayerMessageEvent
{
    public readonly Player Player;
    public readonly string Message;
    public readonly bool IsCentered;

    public PlayerMessageEvent(Player player, string message, bool isCentered = false)
    {
        Player = player;
        Message = message;
        IsCentered = isCentered;
    }
}
