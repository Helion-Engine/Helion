using System;
using System.Collections.Generic;
using System.Linq;
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
            new ExactMatchCheat("Fly mode", "fly", "fly", CheatType.Fly),
        };

        private readonly Dictionary<CheatType, ICheat> m_cheatLookup;
        private string m_currentCheat = "";

        public event EventHandler<ICheat>? CheatActivationChanged;

        public CheatManager()
        {
            m_cheatLookup = Cheats.ToDictionary(cheat => cheat.CheatType);
        }

        public void ActivateCheat(CheatType cheatType)
        {
            if (!m_cheatLookup.ContainsKey(cheatType)) 
                return;
            
            ICheat cheat = m_cheatLookup[cheatType];
            if (cheat.IsToggleCheat)
                cheat.Activated = !cheat.Activated;

            Log.Warn("{0} cheat: {1}", cheat.Activated ? "Activated" : "Deactivated", cheat.CheatName);
            CheatActivationChanged?.Invoke(this, cheat);
        }

        public void HandleInput(ConsumableInput consumableInput)
        {
            foreach (char key in consumableInput.PeekTypedCharacters())
            {
                m_currentCheat += key.ToString();

                if (Cheats.Any(x => x.PartialMatch(m_currentCheat)))
                {
                    ICheat? cheat = Cheats.FirstOrDefault(x => x.IsMatch(m_currentCheat));
                    if (cheat != null)
                    {
                        ActivateCheat(cheat.CheatType);
                        m_currentCheat = "";
                    }
                }
                else
                {
                    m_currentCheat = "";
                }
            }
        }
    }
}