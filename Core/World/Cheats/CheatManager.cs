using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    };

    private readonly Dictionary<CheatType, ICheat> m_cheatLookup;
    private readonly StringBuilder m_currentCheat = new();

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
        var cheat = Cheats.FirstOrDefault(x => command.Equals(x.ConsoleCommand, StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < Cheats.Length; i++)
        {
            if (!command.Equals(Cheats[i].ConsoleCommand, StringComparison.OrdinalIgnoreCase))
                continue;

            ActivateCheat(player, cheat.CheatType);
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
            m_currentCheat.Append(key);
            string cheatString = m_currentCheat.ToString();

            if (AnyPartialMatch(cheatString))
            {
                ICheat? cheat = GetCheatMatch(cheatString);
                if (cheat != null)
                {
                    SetCheat(player, cheat.CheatType, true);
                    if (cheat.ClearTypedCheatString)
                        m_currentCheat.Clear();
                }

                continue;
            }
            
            m_currentCheat.Clear();
        }
    }

    private bool AnyPartialMatch(string cheatString)
    {
        for (int i = 0; i < Cheats.Length; i++)
        {
            if (Cheats[i].PartialMatch(cheatString))
                return true;
        }

        return false;
    }

    private ICheat? GetCheatMatch(string cheatString)
    {
        for (int i = 0; i < Cheats.Length; i++)
        {
            if (Cheats[i].IsMatch(cheatString))
                return Cheats[i];
        }

        return null;
    }
}
