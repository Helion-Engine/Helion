using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helion.Strings;
using Helion.Window;
using Helion.World.Entities.Players;

namespace Helion.World.Cheats;

public class CheatManager
{
    private const string BeholdActivated = "$STSTR_BEHOLDX";

    public static readonly ICheat[] Cheats =
    {
        new LevelCheat("$STSTR_CLEV", "idclev", CheatType.ChangeLevel),
        new LevelCheat("$STSTR_MUS", "idmus", CheatType.ChangeMusic),
        new MultiCodeCheat("$STSTR_NCON", "$STSTR_NCOFF", new string[] { "idclip", "idspispopd" }, "noclip", CheatType.NoClip),
        new ExactMatchCheat(string.Empty, string.Empty, "idmypos", "showposition", CheatType.ShowPosition),
        new AutoMapCheat(string.Empty, "iddt", CheatType.AutomapMode),
        new ExactMatchCheat("$STSTR_DQDON", "$STSTR_DQDOFF", "iddqd", "god", CheatType.God),
        new ExactMatchCheat("$STSTR_FLY", "Fly mode off", "fly", "fly", CheatType.Fly),
        new ExactMatchCheat("$STSTR_DIE", string.Empty, string.Empty, "kill", CheatType.Kill, canToggle: false),
        new ExactMatchCheat("$STSTR_MONSTERSKILLED", string.Empty, string.Empty, "killmonsters", CheatType.KillAllMonsters, canToggle: false),
        new ExactMatchCheat("$STSTR_RES", string.Empty, string.Empty, "resurrect", CheatType.Ressurect, canToggle: false),
        new ExactMatchCheat("$STSTR_FAADDED", string.Empty, "idfa", "idfa", CheatType.GiveAllNoKeys, canToggle: false),
        new ExactMatchCheat("$STSTR_KFAADDED", string.Empty, "idkfa", "idkfa", CheatType.GiveAll, canToggle: false),
        new ExactMatchCheat("$STSTR_CHOPPERS", string.Empty, "idchoppers", CheatType.Chainsaw, canToggle: false),

        new ExactMatchCheat("$STSTR_BEHOLD", string.Empty, "idbehold", CheatType.Behold, canToggle: false,
            clearTypedCheatString: false),
        new ExactMatchCheat(BeholdActivated, string.Empty, "idbeholdr", CheatType.BeholdRadSuit, canToggle: false),
        new ExactMatchCheat(BeholdActivated, string.Empty, "idbeholdi", CheatType.BeholdPartialInvisibility, canToggle: false),
        new ExactMatchCheat(BeholdActivated, string.Empty, "idbeholdv", CheatType.BeholdInvulnerability, canToggle: false),
        new ExactMatchCheat(BeholdActivated, string.Empty, "idbeholda", CheatType.BeholdComputerAreaMap, canToggle: false),
        new ExactMatchCheat(BeholdActivated, string.Empty, "idbeholdl", CheatType.BeholdLightAmp, canToggle: false),
        new ExactMatchCheat(BeholdActivated, string.Empty, "idbeholds", CheatType.BeholdBerserk, canToggle: false),

        new ExactMatchCheat("Exit", string.Empty, "exitlevel", "exitlevel", CheatType.Exit, canToggle: false),
        new ExactMatchCheat("Exit Secret", string.Empty, "exitlevelsecret", "exitlevelsecret", CheatType.ExitSecret, canToggle: false),
    };

    private readonly Dictionary<CheatType, ICheat> m_cheatLookup;
    private string m_currentCheat = StringBuffer.GetString(128);

    public event EventHandler<CheatEventArgs>? CheatActivationChanged;

    public CheatManager()
    {
        m_cheatLookup = Cheats.ToDictionary(cheat => cheat.CheatType);
    }

    public static void SetCheatCode(CheatType type, string code, int index = 0)
    {
        for (int i = 0; i < Cheats.Length; i++)
        {
            if (Cheats[i].CheatType != type)
                continue;

            Cheats[i].SetCode(code, index);
            return;
        }
    }

    public void ActivateCheat(Player player, CheatType cheatType, int levelNumber = -1)
    {
        if (cheatType == CheatType.ChangeLevel || cheatType == CheatType.ChangeMusic)
        {
            ActivateLevelCheat(player, cheatType, levelNumber);
            return;
        }

        SetCheat(player, cheatType, true);
    }

    public void DeactivateCheat(Player player, CheatType cheatType) =>
        SetCheat(player, cheatType, false);

    private void SetCheat(Player player, CheatType cheatType, bool activate)
    {
        if (!m_cheatLookup.ContainsKey(cheatType) || (!activate && !player.Cheats.IsCheatActive(cheatType)))
            return;

        ICheat cheat = m_cheatLookup[cheatType];
        if (cheat.IsToggleCheat)
            cheat.SetActivated(player);

        CheatActivationChanged?.Invoke(this, new CheatEventArgs(player, cheat));
    }

    private void ActivateLevelCheat(Player player, CheatType cheatType, int levelNumber)
    {
        if (!m_cheatLookup.TryGetValue(cheatType, out ICheat? cheat))
            return;

        if (cheat is not LevelCheat levelCheat)
            return;

        levelCheat.SetLeveNumber(levelNumber);
        SetCheat(player, cheatType, true);
    }

    public bool HandleCommand(Player player, string command)
    {
        for (int i = 0; i < Cheats.Length; i++)
        {
            if (!command.Equals(Cheats[i].ConsoleCommand, StringComparison.OrdinalIgnoreCase))
                continue;

            ActivateCheat(player, Cheats[i].CheatType);
            return true;
        }

        return false;
    }

    public void HandleInput(Player player, IConsumableInput input)
    {
        var characters = input.ConsumeTypedCharacters();
        for (int i = 0; i < characters.Length; i++)
        {
            char key = characters[i];
            m_currentCheat = StringBuffer.Append(m_currentCheat, key);

            if (AnyPartialMatch(m_currentCheat))
            {
                ICheat? cheat = GetCheatMatch(m_currentCheat);
                if (cheat != null)
                {
                    SetCheat(player, cheat.CheatType, true);
                    if (cheat.ClearTypedCheatString)
                        StringBuffer.Clear(m_currentCheat);
                }

                continue;
            }

            StringBuffer.Clear(m_currentCheat);
        }
    }

    private bool AnyPartialMatch(string cheatString)
    {
        ReadOnlySpan<char> cheatSpan = cheatString.AsSpan(0, StringBuffer.StringLength(cheatString));
        for (int i = 0; i < Cheats.Length; i++)
        {
            if (Cheats[i].PartialMatch(cheatSpan))
                return true;
        }

        return false;
    }

    private ICheat? GetCheatMatch(string cheatString)
    {
        ReadOnlySpan<char> cheatSpan = cheatString.AsSpan(0, StringBuffer.StringLength(cheatString));
        for (int i = 0; i < Cheats.Length; i++)
        {
            if (Cheats[i].IsMatch(cheatSpan))
                return Cheats[i];
        }

        return null;
    }
}
