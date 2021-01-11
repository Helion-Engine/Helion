using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helion.Input;
using NLog;

namespace Helion.World.Cheats
{
    public class CheatManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly ICheat[] Cheats = 
        {
            new ChangeLevelCheat(),
            new ExactMatchCheat("No clipping mode", "idclip", "noclip", CheatType.NoClip),
            new ExactMatchCheat("God mode", "iddqd", "god", CheatType.God),
            new ExactMatchCheat("Fly mode", "fly", "fly", CheatType.Fly),
            new ExactMatchCheat("Resurrect", "resurrect", "resurrect", CheatType.Ressurect, false),
            new ExactMatchCheat("Ammo (no keys) added", "idfa", "idfa", CheatType.GiveAllNoKeys),
            new ExactMatchCheat("Very happy ammo added", "idkfa", "idkfa", CheatType.GiveAll),
        };

        private readonly Dictionary<CheatType, ICheat> m_cheatLookup;
        private StringBuilder m_currentCheat = new StringBuilder();

        public event EventHandler<ICheat>? CheatActivationChanged;

        public static CheatManager Instance { get; } = new CheatManager();

        public CheatManager()
        {
            m_cheatLookup = Cheats.ToDictionary(cheat => cheat.CheatType);
        }

        public void Clear()
        {
            foreach (var value in m_cheatLookup.Values)
                value.Activated = false;
        }

        public void ActivateCheat(CheatType cheatType)
        {
            if (!m_cheatLookup.ContainsKey(cheatType)) 
                return;
            
            ICheat cheat = m_cheatLookup[cheatType];
            if (cheat.IsToggleCheat)
                cheat.Activated = !cheat.Activated;

            Log.Warn("{0} cheat: {1}", cheat.Activated || !cheat.IsToggleCheat ? "Activated" : "Deactivated", cheat.CheatName);
            CheatActivationChanged?.Invoke(this, cheat);
        }

        public bool HandleCommand(string command)
        {
            var cheat = Cheats.FirstOrDefault(x => command.Equals(x.ConsoleCommand, StringComparison.OrdinalIgnoreCase));

            if (cheat != null)
            {
                ActivateCheat(cheat.CheatType);
                return true;
            }

            return false;
        }

        public void HandleInput(ConsumableInput consumableInput)
        {
            foreach (char key in consumableInput.PeekTypedCharacters())
            {
                m_currentCheat.Append(key);
                string cheatString = m_currentCheat.ToString();

                if (Cheats.Any(x => x.PartialMatch(cheatString)))
                {
                    ICheat? cheat = Cheats.FirstOrDefault(x => x.IsMatch(cheatString));
                    if (cheat != null)
                    {
                        ActivateCheat(cheat.CheatType);
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