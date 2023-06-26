using Helion.World.Entities.Players;

namespace Helion.World.Cheats;

public readonly struct CheatEventArgs
{
    public readonly Player Player;
    public readonly ICheat Cheat;

    public CheatEventArgs(Player player, ICheat cheat)
    {
        Player = player;
        Cheat = cheat;
    }
}
