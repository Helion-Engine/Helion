namespace Helion.World.Cheats
{
    public class ChangeLevelCheat : ICheat
    {
        private const string Code = "idclev";

        public string CheatName => string.Empty;
        public string? ConsoleCommand => null;
        public CheatType CheatType => CheatType.ChangeLevel;
        public bool Activated { get; set; }
        public bool IsToggleCheat => false;
        public int LevelNumber { get; private set; } = 1;

        public bool IsMatch(string str)
        {
            if (PartialMatch(str) && str.Length == Code.Length + 2)
            {
                if (char.IsDigit(str[str.Length - 1]) && char.IsDigit(str[str.Length - 2]))
                {
                    string digits = str.Substring(str.Length - 2, 2);
                    if (int.TryParse(digits, out int levelNumber))
                    {
                        LevelNumber = levelNumber;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool PartialMatch(string str)
        {
            if (Code.StartsWith(str))
                return true;
            return str.Length <= Code.Length + 2 && str.StartsWith(Code);
        }
    }
}