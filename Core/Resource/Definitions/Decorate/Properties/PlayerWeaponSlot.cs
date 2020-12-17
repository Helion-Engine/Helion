using System.Collections.Generic;

namespace Helion.Resource.Definitions.Decorate.Properties
{
    public class PlayerWeaponSlot
    {
        public string Slot;
        public List<string> Weapons;

        public PlayerWeaponSlot(string slot, List<string> weapons)
        {
            Slot = slot;
            Weapons = weapons;
        }
    }
}