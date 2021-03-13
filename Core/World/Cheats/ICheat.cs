namespace Helion.World.Cheats
{
    public interface ICheat
    {
        string CheatName { get; }
        string? ConsoleCommand { get; }
        CheatType CheatType { get; }
        bool IsToggleCheat { get; }
        
        bool IsMatch(string str);
        bool PartialMatch(string str);
    }
}
