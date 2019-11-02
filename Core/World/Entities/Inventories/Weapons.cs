using System.Collections.Generic;
using System.Linq;
using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Players;

namespace Helion.World.Entities.Inventories
{
    /// <summary>
    /// A collection of weapons that a player owns.
    /// </summary>
    public class Weapons
    {
        private readonly Dictionary<int, List<Weapon>> m_weaponSlots = new Dictionary<int, List<Weapon>>();

        /// <summary>
        /// Adds a new weapon to a slot.
        /// </summary>
        /// <param name="slot">The slot for the weapon.</param>
        /// <param name="definition">The definition of the weapon.</param>
        /// <param name="owner">The player that owns this weapon.</param>
        /// <param name="entityManager">The entity manager that the weapon will
        /// use when being fired.</param>
        public void Add(int slot, EntityDefinition definition, Player owner, EntityManager entityManager)
        {
            if (OwnsWeapon(definition.Name, slot))
                return;
            
            Weapon weapon = new Weapon(definition, owner, entityManager);
            if (m_weaponSlots.TryGetValue(slot, out List<Weapon>? weapons))
                weapons.Add(weapon);
            m_weaponSlots[slot] = new List<Weapon> { weapon };
        }
        
        /// <summary>
        /// Removes a weapon with the name provided.
        /// </summary>
        /// <param name="name">The case-insensitive name of the weapon.</param>
        public void Remove(CIString name)
        {
            foreach (var weapons in m_weaponSlots.Values)
                weapons.RemoveAll(w => w.Definition.Name == name);
        }

        private bool OwnsWeapon(CIString name, int slot)
        {
            if (m_weaponSlots.TryGetValue(slot, out List<Weapon>? weapons))
                if (weapons.Any(w => w.Definition.Name == name))
                    return true;
            return false;
        }
    }
}