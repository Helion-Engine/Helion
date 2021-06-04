using Helion.World.Entities.Players;

namespace Helion.World.Cheats
{
    class AutoMapCheat : ExactMatchCheat, ICheat
    {
        public AutoMapCheat(string name, string code, CheatType cheatType) :
            base(name, string.Empty, code, cheatType)
        {

        }

        void ICheat.SetActivated(Player player)
        {
            if (player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLinesAndThings))
            {
                player.Cheats.SetCheatInactive(CheatType.AutoMapModeShowAllLinesAndThings);
            }
            else if (player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLines))
            {
                player.Cheats.SetCheatInactive(CheatType.AutoMapModeShowAllLines);
                player.Cheats.SetCheatActive(CheatType.AutoMapModeShowAllLinesAndThings);
            }
            else
            {
                player.Cheats.SetCheatActive(CheatType.AutoMapModeShowAllLines);
            }
        }
    }
}
