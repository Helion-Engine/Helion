using System;

namespace Helion.World.Cheats
{
    public class ExactMatchCheat : ICheat
    {
        private string m_code;

        public string CheatOn { get; }
        public string CheatOff { get; }
        public string? ConsoleCommand { get; }
        public CheatType CheatType { get; }
        public bool Activated { get; set; }
        public bool IsToggleCheat { get; private set; }
        public bool ClearTypedCheatString { get; private set; }
        
        public ExactMatchCheat(string on, string off, string code, CheatType cheatType, bool canToggle = true,
            bool clearTypedCheatString = true) : 
            this(on, off, code, "", cheatType, canToggle, clearTypedCheatString)
        {
        }

        public ExactMatchCheat(string on, string off, string code, string consoleCommand, CheatType cheatType, bool canToggle = true,
            bool clearTypedCheatString = true)
        {
            CheatOn = on;
            CheatOff = off;
            ConsoleCommand = consoleCommand;
            m_code = code;
            CheatType = cheatType;
            IsToggleCheat = canToggle;
            ClearTypedCheatString = clearTypedCheatString;
        }

        public void SetCode(string code, int index = 0) => m_code = code;

        public bool IsMatch(string str) => m_code.Equals(str, StringComparison.InvariantCultureIgnoreCase);

        public bool PartialMatch(string str) => m_code.StartsWith(str, StringComparison.InvariantCultureIgnoreCase);
    }
}