using System;

namespace Helion.World.Cheats
{
    public class ExactMatchCheat : ICheat
    {
        private readonly string m_code;

        public string CheatName { get; }
        public string? ConsoleCommand { get; }
        public CheatType CheatType { get; }
        public bool Activated { get; set; }
        public bool IsToggleCheat { get; private set; }
        public bool ClearTypedCheatString { get; private set; }
        
        public ExactMatchCheat(string name, string code, CheatType cheatType, bool canToggle = true,
            bool clearTypedCheatString = true) : 
            this(name, code, "", cheatType, canToggle, clearTypedCheatString)
        {
        }

        public ExactMatchCheat(string name, string code, string consoleCommand, CheatType cheatType, bool canToggle = true,
            bool clearTypedCheatString = true)
        {
            CheatName = name;
            ConsoleCommand = consoleCommand;
            m_code = code;
            CheatType = cheatType;
            IsToggleCheat = canToggle;
            ClearTypedCheatString = clearTypedCheatString;
        }

        public bool IsMatch(string str) => m_code.Equals(str, StringComparison.InvariantCultureIgnoreCase);

        public bool PartialMatch(string str) => m_code.StartsWith(str, StringComparison.InvariantCultureIgnoreCase);
    }
}