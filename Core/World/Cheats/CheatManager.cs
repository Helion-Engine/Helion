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
        private const string BeholdActivated = "Power-up Toggled";

        private static readonly ICheat[] Cheats =
        {
            new LevelCheat(string.Empty, "idclev", CheatType.ChangeLevel),
            new LevelCheat("Change music", "idmus", CheatType.ChangeMusic),
            new MultiCodeCheat("No clipping mode", new string[] { "idclip", "idspispopd" }, "noclip", CheatType.NoClip),
            new ExactMatchCheat(string.Empty, "idmypos", CheatType.ShowPosition),
            new AutoMapCheat(string.Empty, "iddt", CheatType.AutomapMode),
            new ExactMatchCheat("Degreelessness mode", "iddqd", "god", CheatType.God),
            new ExactMatchCheat("Fly mode", "fly", "fly", CheatType.Fly),
            new ExactMatchCheat("Die", "kill", "kill", CheatType.Kill, canToggle: false),
            new ExactMatchCheat("Resurrect", "resurrect", "resurrect", CheatType.Ressurect, canToggle: false),
            new ExactMatchCheat("Ammo (no keys) added", "idfa", "idfa", CheatType.GiveAllNoKeys, canToggle: false),
            new ExactMatchCheat("Very happy ammo added", "idkfa", "idkfa", CheatType.GiveAll, canToggle: false),
            new ExactMatchCheat("... Doesn't Suck - GM", "idchoppers", CheatType.Chainsaw, canToggle: false),

            new ExactMatchCheat("inVuln, Str, Inviso, Rad, Allmap, or Lite-amp", "idbehold", CheatType.Behold, canToggle: false,
                clearTypedCheatString: false),
            new ExactMatchCheat(BeholdActivated, "idbeholdr", CheatType.BeholdRadSuit, canToggle: false),
            new ExactMatchCheat(BeholdActivated, "idbeholdi", CheatType.BeholdPartialInvisibility, canToggle: false),
            new ExactMatchCheat(BeholdActivated, "idbeholdv", CheatType.BeholdInvulnerability, canToggle: false),
            new ExactMatchCheat(BeholdActivated, "idbeholda", CheatType.BeholdComputerAreaMap, canToggle: false),
            new ExactMatchCheat(BeholdActivated, "idbeholdl", CheatType.BeholdLightAmp, canToggle: false),
            new ExactMatchCheat(BeholdActivated, "idbeholds", CheatType.BeholdBerserk, canToggle: false),
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