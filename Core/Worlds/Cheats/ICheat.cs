namespace Helion.Worlds.Cheats
{
    public interface ICheat
    {
        string CheatName { get; }
        string? ConsoleCommand { get; }
        CheatType CheatType { get; }
        bool IsToggleCheat { get; }
        bool Activated { get; set; }

        bool IsMatch(string str);
        bool PartialMatch(string str);
    }
}
