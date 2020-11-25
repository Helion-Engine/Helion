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
        public bool IsToggleCheat => true;
        
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

        public bool IsMatch(string str) => m_code.Equals(str, StringComparison.InvariantCultureIgnoreCase);

        public bool PartialMatch(string str) => m_code.StartsWith(str, StringComparison.InvariantCultureIgnoreCase);
    }
}