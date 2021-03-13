using Helion.World.Cheats;
using System.Collections.Generic;

namespace Helion.World.Entities.Players
{
    public class PlayerCheats
    {
        private readonly List<CheatType> m_cheats = new List<CheatType>();

        public bool IsCheatActive(CheatType cheatType) => m_cheats.Contains(cheatType);

        public void SetCheatActive(CheatType cheatType)
        {
            if (!IsCheatActive(cheatType))
                m_cheats.Add(cheatType);
        }

        public void SetCheatInactive(CheatType cheatType) =>
            m_cheats.Remove(cheatType);

        public void ClearCheats() => m_cheats.Clear();

        public IReadOnlyList<CheatType> GetActiveCheats() => m_cheats.AsReadOnly();
    }
}
