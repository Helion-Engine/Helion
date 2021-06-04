using Helion.World.Entities.Players;

namespace Helion.World.Cheats
{
    public interface ICheat
    {
        string CheatOn { get; }
        string CheatOff { get; }
        string? ConsoleCommand { get; }
        CheatType CheatType { get; }
        bool IsToggleCheat { get; }
        bool ClearTypedCheatString { get; }

        bool IsMatch(string str);
        bool PartialMatch(string str);

        public virtual void SetActivated(Player player)
        {
            if (player.Cheats.IsCheatActive(CheatType))
                player.Cheats.SetCheatInactive(CheatType);
            else
                player.Cheats.SetCheatActive(CheatType);
        }
    }
}
