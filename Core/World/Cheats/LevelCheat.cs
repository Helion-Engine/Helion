using System;

namespace Helion.World.Cheats;

public class LevelCheat : ICheat
{
    public string CheatOn { get; private set; }
    public string CheatOff => string.Empty;
    public string? ConsoleCommand => null;
    public CheatType CheatType { get; private set; }
    public bool Activated { get; set; }
    public bool IsToggleCheat => false;
    public bool ClearTypedCheatString => true;
    public int LevelNumber { get; private set; } = 1;

    private string m_code;

    public LevelCheat(string name, string code, CheatType cheatType)
    {
        m_code = code;
        CheatOn = name;
        CheatType = cheatType;
    }

    public void SetLeveNumber(int levelNumber) => LevelNumber = Math.Clamp(levelNumber, 1, int.MaxValue);

    public void SetCode(string code, int index = 0) => m_code = code;

    public bool IsMatch(string str)
    {
        if (PartialMatch(str) && str.Length == m_code.Length + 2 && char.IsDigit(str[^1]) && char.IsDigit(str[^2]))
        {
            string digits = str.Substring(str.Length - 2, 2);
            if (int.TryParse(digits, out int levelNumber))
            {
                LevelNumber = levelNumber;
                return true;
            }
        }

        return false;
    }

    public bool PartialMatch(string str)
    {
        if (m_code.StartsWith(str))
            return true;
        return str.Length <= m_code.Length + 2 && str.StartsWith(m_code);
    }
}
