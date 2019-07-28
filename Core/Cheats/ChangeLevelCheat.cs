namespace Helion.Cheats
{
    public class ChangeLevelCheat : ICheat
    {
        private readonly string m_code = "idclev";

        public string CheatName => string.Empty;

        public string? ConsoleCommand => null;

        public CheatType CheatType => CheatType.ChangeLevel;

        public bool Activated { get; set; }

        public bool IsToggleCheat => false;

        public string? LevelDigits { get; private set; }

        public bool IsMatch(string str)
        {
            if (PartialMatch(str) && str.Length == m_code.Length + 2)
            {
                if (char.IsDigit(str[str.Length - 1]) && char.IsDigit(str[str.Length - 2]))
                {
                    LevelDigits = str.Substring(str.Length - 2, 2);
                    return true;
                }
            }

            return false;
        }

        public bool PartialMatch(string str)
        {
            if (m_code.StartsWith(str))
                return true;
            if (str.Length <= m_code.Length + 2 && str.StartsWith(m_code))
                return true;

            return false;
        }
    }
}
