using System.Collections.Generic;

namespace Helion.World.Entities.Definition.Properties.Components;

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

