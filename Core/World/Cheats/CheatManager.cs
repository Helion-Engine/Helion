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
        private static readonly ICheat[] Cheats =
        {
            new ChangeLevelCheat(),
            new ExactMatchCheat("No clipping mode", "idclip", "noclip", CheatType.NoClip),
            new ExactMatchCheat("God mode", "iddqd", "god", CheatType.God),
            new ExactMatchCheat("Fly mode", "fly", "fly", CheatType.Fly),
            new ExactMatchCheat("Resurrect", "resurrect", "resurrect", CheatType.Ressurect, canToggle: false),
            new ExactMatchCheat("Ammo (no keys) added", "idfa", "idfa", CheatType.GiveAllNoKeys, canToggle: false),
            new ExactMatchCheat("Very happy ammo added", "idkfa", "idkfa", CheatType.GiveAll, canToggle: false),
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
            {
                if (player.Cheats.IsCheatActive(cheatType))
                    player.Cheats.SetCheatInactive(cheatType);
                else
                    player.Cheats.SetCheatActive(cheatType);
            }

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