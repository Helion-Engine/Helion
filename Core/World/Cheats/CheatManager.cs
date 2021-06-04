using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helion.Input;
using Helion.World.Entities.Players;

namespace Helion.World.Cheats
{
    public class CheatManager
    {
        private const string BeholdActivated = "$STSTR_BEHOLDX";

        private static readonly ICheat[] Cheats =
        {
            new LevelCheat("$STSTR_CLEV", "idclev", CheatType.ChangeLevel),
            new LevelCheat("$STSTR_MUS", "idmus", CheatType.ChangeMusic),
            new MultiCodeCheat("$STSTR_NCON", "$STSTR_NCOFF", new string[] { "idclip", "idspispopd" }, "noclip", CheatType.NoClip),
            new ExactMatchCheat(string.Empty, string.Empty, "idmypos", CheatType.ShowPosition),
            new AutoMapCheat(string.Empty, "iddt", CheatType.AutomapMode),
            new ExactMatchCheat("$STSTR_DQDON", "$STSTR_DQDOFF", "iddqd", "god", CheatType.God),
            new ExactMatchCheat("$STSTR_FLY", "Fly mode off", "fly", "fly", CheatType.Fly),
            new ExactMatchCheat("$STSTR_DIE", string.Empty, "kill", "kill", CheatType.Kill, canToggle: false),
            new ExactMatchCheat("$STSTR_RES", string.Empty, "resurrect", "resurrect", CheatType.Ressurect, canToggle: false),
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
        private readonly StringBuilder m_currentCheat = new StringBuilder();

        public event EventHandler<CheatEventArgs>? CheatActivationChanged;

        public static CheatManager Instance { get; } = new CheatManager();

        public CheatManager()
        {
            m_cheatLookup = Cheats.ToDictionary(cheat => cheat.CheatType);
        }

        public void ActivateCheat(Player player, CheatType cheatType)
        {
            if (!m_cheatLookup.ContainsKey(cheatType))
                return;

            ICheat cheat = m_cheatLookup[cheatType];
            if (cheat.IsToggleCheat)
                cheat.SetActivated(player);

            CheatActivationChanged?.Invoke(this, new CheatEventArgs(player, cheat));
        }

        public bool HandleCommand(Player player, string command)
        {
            var cheat = Cheats.FirstOrDefault(x => command.Equals(x.ConsoleCommand, StringComparison.OrdinalIgnoreCase));

            if (cheat != null)
            {
                ActivateCheat(player, cheat.CheatType);
                return true;
            }

            return false;
        }

        public void HandleInput(Player player, InputEvent input)
        {
            foreach (char key in input.GetTypedCharacters())
            {
                m_currentCheat.Append(key);
                string cheatString = m_currentCheat.ToString();

                if (Cheats.Any(x => x.PartialMatch(cheatString)))
                {
                    ICheat? cheat = Cheats.FirstOrDefault(x => x.IsMatch(cheatString));
                    if (cheat != null)
                    {
                        ActivateCheat(player, cheat.CheatType);
                        if (cheat.ClearTypedCheatString)
                            m_currentCheat.Clear();
                    }
                }
                else
                {
                    m_currentCheat.Clear();
                }
            }
        }
    }
}