using Helion.World.Entities.Players;

namespace Helion.World;

public record struct DisplayMessageArgs(string Message, Player? Player, Player? Other, bool IsCentered = false, bool ForAllPlayers = false);
