namespace Helion.Cheats
{
    public enum CheatType
    {
        GiveItem,
        Chainsaw,
        NoClip,
        ChangeLevel,
        God,
        Automap,
        GiveAllNoKeys,
        GiveAll,
        ChangeMusic,
        ShowPosition
    }

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
