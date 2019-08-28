namespace Helion.Cheats
{
    public class ExactMatchCheat : ICheat
    {
        private readonly string m_code;

        public ExactMatchCheat(string name, string code, CheatType cheatType) : 
            this(name, code, "", cheatType)
        {
        }

        public ExactMatchCheat(string name, string code, string consoleCommand, CheatType cheatType)
        {
            CheatName = name;
            ConsoleCommand = consoleCommand;
            m_code = code;
            CheatType = cheatType;
        }

        public string CheatName { get; private set; }

        public string? ConsoleCommand { get; private set; }

        public CheatType CheatType { get; private set; }

        public bool Activated { get; set; }

        public bool IsToggleCheat => true;

        public bool IsMatch(string str) => m_code.Equals(str);

        public bool PartialMatch(string str) => m_code.StartsWith(str);
    }
}