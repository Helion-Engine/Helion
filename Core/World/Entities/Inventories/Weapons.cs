using System.Collections.Generic;
using System.Linq;
using Helion.Models;
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
        private readonly Dictionary<int, Dictionary<int, Weapon>> m_weaponSlots = new();
        private readonly Dictionary<CIString, (int, int)> m_weaponSlotLookup = new();
        private readonly List<CIString> m_weaponNames = new();

        public Weapons(Dictionary<int, List<string>> weaponSlots)
        {
            foreach (var item in weaponSlots)
            {
                int subslot = 0;
                foreach (string weapon in item.Value)
                {
                    m_weaponNames.Add(weapon);
                    m_weaponSlotLookup.Add(weapon, (item.Key, subslot++));
                }
            }
        }

        public (int, int) GetWeaponSlot(EntityDefinition definition)
        {
            if (m_weaponSlotLookup.TryGetValue(definition.Name, out (int, int) slot))
                return slot;

            return DefaultSlot;
        }

        public IList<CIString> GetWeaponDefinitionNames() => m_weaponNames.AsReadOnly();

        public List<string> GetOwnedWeaponNames()
        {
            List<string> weapons = new List<string>();
            foreach (var item in m_weaponSlots)
            {
                foreach (var subItem in item.Value)
                    weapons.Add(subItem.Value.Definition.Name.ToString());
            }

            return weapons;
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
        /// <param name="frameStateModel">Frame state model to apply.</param>
        /// <param name="flashStateModel">Flash state model to apply.</param>
        public Weapon? Add(EntityDefinition definition, Player owner, EntityManager entityManager,
            FrameStateModel? frameStateModel = null, FrameStateModel? flashStateModel = null)
        {
            if (OwnsWeapon(definition.Name))
                return null;

            var (slot, subslot) = GetWeaponSlot(definition);

            Weapon weapon = new Weapon(definition, owner, entityManager, frameStateModel, flashStateModel);
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

        public List<Weapon> GetWeapons()
        {
            List<Weapon> allWeapons = new List<Weapon>();
            foreach (var weapons in m_weaponSlots.Values)
                allWeapons.AddRange(weapons.Values);
            return allWeapons;
        }

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

            if (m_weaponSlots.TryGetValue(slot, out Dictionary<int, Weapon>? weapons) &&
                weapons.TryGetValue(subslot, out Weapon? weapon))
            {
                return weapon;
            }

            return null;
        }
    }
}