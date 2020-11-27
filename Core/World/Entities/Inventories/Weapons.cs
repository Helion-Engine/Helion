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
        private const int MinSlot = 1;
        private const int MaxSlot = 7;
        private static readonly (int, int) DefaultSlot = (-1, -1);
        private readonly Dictionary<int, Dictionary<int, Weapon>> m_weaponSlots = new Dictionary<int, Dictionary<int, Weapon>>();

        // TODO move and implement based on GameInfo
        public static (int, int) GetWeaponSlot(EntityDefinition definition)
        {
            if (definition.Name == "CHAINSAW")
                return (1, 1);
            if (definition.Name == "FIST")
                return (1, 0);
            else if (definition.Name == "PISTOL")
                return (2, 0);
            else if (definition.Name == "SHOTGUN")
                return (3, 0);
            else if (definition.Name == "SUPERSHOTGUN")
                return (3, 1);
            else if (definition.Name == "CHAINGUN")
                return (4, 0);
            else if (definition.Name == "ROCKETLAUNCHER")
                return (5, 0);
            else if (definition.Name == "PLASMARIFLE")
                return (6, 0);
            else if (definition.Name == "BFG9000")
                return (7, 0);

            return DefaultSlot;
        }

        public static CIString[] GetWeaponDefinitionNames()
        {
            return new CIString[]
            {
                "CHAINSAW",
                "FIST",
                "PISTOL",
                "SHOTGUN",
                "SUPERSHOTGUN",
                "CHAINGUN",
                "ROCKETLAUNCHER",
                "PLASMARIFLE",
                "BFG9000"
            };
        }

        public (int, int) GetNextSlot(Player player) => CycleSlot(player, true);
        public (int, int) GetPreviousSlot(Player player) => CycleSlot(player, false);

        private (int, int) CycleSlot(Player player, bool next)
        {
            int slot = player.WeaponSlot;
            int startSlot = slot;

            int subslot = CycleSubSlot(player, next);
            if (subslot != -1)
                return (slot, subslot);

            if (next)
                slot = GetSlot(++slot, MinSlot, MaxSlot);
            else
                slot = GetSlot(--slot, MinSlot, MaxSlot);

            while (slot != startSlot)
            {
                if (next)
                    subslot = GetFirstSubSlot(slot);
                else
                    subslot = GetSubSlots(slot) - 1;

                Weapon? weapon = GetWeapon(player, slot, subslot);
                if (weapon != null)
                    return (slot, subslot);

                if (next)
                    slot = GetSlot(++slot, MinSlot, MaxSlot);
                else
                    slot = GetSlot(--slot, MinSlot, MaxSlot);
            }

            return DefaultSlot;
        }

        private int CycleSubSlot(Player player, bool next)
        {
            int subslot = player.WeaponSubSlot;
            if (next)
                subslot++;
            else
                subslot--;

            if (subslot < 0 || subslot > GetSubSlots(player.WeaponSlot) - 1)
                return -1;

            return subslot;
        }

        private static int GetSlot(int slot, int min, int max)
        {
            if (slot < min)
                slot = max;
            if (slot > max)
                slot = min;
            return slot;
        }

        /// <summary>
        /// Adds a new weapon to a slot.
        /// </summary>
        /// <param name="definition">The definition of the weapon.</param>
        /// <param name="owner">The player that owns this weapon.</param>
        /// <param name="entityManager">The entity manager that the weapon will
        /// use when being fired.</param>
        public Weapon? Add(EntityDefinition definition, Player owner, EntityManager entityManager)
        {
            if (OwnsWeapon(definition.Name))
                return null;

            var (slot, subslot) = GetWeaponSlot(definition);

            Weapon weapon = new Weapon(definition, owner, entityManager);
            if (!m_weaponSlots.TryGetValue(slot, out Dictionary<int, Weapon>? weapons))
            {
                m_weaponSlots[slot] = new Dictionary<int, Weapon>();
                weapons = m_weaponSlots[slot];
            }

            weapons.Add(subslot, weapon);

            return weapon;
        }
        
        /// <summary>
        /// Removes a weapon with the name provided.
        /// </summary>
        /// <param name="name">The case-insensitive name of the weapon.</param>
        public void Remove(CIString name)
        {
            foreach (var weapons in m_weaponSlots.Values)
                foreach (var item in weapons.Where(kv => kv.Value.Definition.Name == name))
                    m_weaponSlots.Remove(item.Key);
        }

        public int GetFirstSubSlot(int slot)
        {
            if (m_weaponSlots.TryGetValue(slot, out Dictionary<int, Weapon>? weapons) && weapons.Count > 0)
                return weapons.First().Key;
            return -1;
        }

        public int GetSubSlots(int slot)
        {
            if (m_weaponSlots.TryGetValue(slot, out Dictionary<int, Weapon>? weapons))
                return weapons.Count;
            return 0;
        }

        public bool OwnsWeapon(CIString name) => GetWeapon(name) != null;

        public Weapon? GetWeapon(CIString name)
        {
            foreach (var weapons in m_weaponSlots.Values)
            {
                foreach (var weapon in weapons.Values)
                {
                    if (weapon.Definition.Name == name)
                        return weapon;
                }
            }

            return null;
        }

        public Weapon? GetWeapon(Player player, int slot, int subslot = -1)
        {
            if (slot == player.WeaponSlot && subslot == -1)
                subslot = GetSlot(player.WeaponSubSlot + 1, 0, GetSubSlots(slot));
            else if (subslot == -1)
                subslot = GetSubSlots(slot) - 1;

            if (m_weaponSlots.TryGetValue(slot, out Dictionary<int, Weapon>? weapons))
            {
                if (weapons.TryGetValue(subslot, out Weapon? weapon))
                    return weapon;
            }

            return null;
        }
    }
}