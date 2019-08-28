using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Input;

namespace Helion.World.Cheats
{
    public class CheatManager
    {
        private readonly ICheat[] m_cheats = 
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
            m_cheatLookup = m_cheats.ToDictionary(x => x.CheatType);
        }

        public void ActivateCheat(CheatType cheatType)
        {
            if (!m_cheatLookup.ContainsKey(cheatType)) 
                return;
            
            ICheat cheat = m_cheatLookup[cheatType];
            if (cheat.IsToggleCheat)
                cheat.Activated = !cheat.Activated;

            CheatActivationChanged?.Invoke(this, cheat);
        }

        public void HandleInput(ConsumableInput consumableInput)
        {
            foreach (char key in consumableInput.PeekTypedCharacters())
            {
                m_currentCheat += key.ToString();

                if (m_cheats.Any(x => x.PartialMatch(m_currentCheat)))
                {
                    ICheat? cheat = m_cheats.FirstOrDefault(x => x.IsMatch(m_currentCheat));
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