using Helion.World.Entities.Players;
using System;

namespace Helion.World.Cheats;

public interface ICheat
{
    string CheatOn { get; }
    string CheatOff { get; }
    string? ConsoleCommand { get; }
    CheatType CheatType { get; }
    bool IsToggleCheat { get; }
    bool ClearTypedCheatString { get; }

    void SetCode(string code, int index = 0);
    bool IsMatch(ReadOnlySpan<char> str);
    bool PartialMatch(ReadOnlySpan<char> str);

    public virtual void SetActivated(Player player)
    {
        if (player.Cheats.IsCheatActive(CheatType))
            player.Cheats.SetCheatInactive(CheatType);
        else
            player.Cheats.SetCheatActive(CheatType);
    }
}
