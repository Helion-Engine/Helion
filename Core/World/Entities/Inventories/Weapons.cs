using System;
using System.Collections.Generic;
using Helion.Models;
using Helion.Util.Container;
using Helion.Util.Loggers;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Players;

namespace Helion.World.Entities.Inventories;

/// <summary>
/// A collection of weapons that a player owns.
/// </summary>
public sealed class Weapons
{
    private const int MinSlot = 1;
    private const int MaxSlot = 7;
    private static readonly WeaponSlot DefaultSlot = new(-1, -1);
    private readonly Dictionary<int, WeaponSlot> m_weaponSlotLookup = [];
    private readonly List<string> m_weaponNames = [];
    private readonly List<Weapon> m_ownedWeapons = [];
    private readonly LookupArray<Weapon?> m_ownedWeaponsById = new();
    private readonly Comparison<Weapon> m_weaponSelectionOrderCompare = new(WeaponSelectionOrderCompare);

    public event EventHandler? WeaponsCleared;
    public event EventHandler<Weapon>? WeaponRemoved;

    public Weapons(Dictionary<int, List<string>> weaponSlots, EntityDefinitionComposer composer)
    {
        foreach (var item in weaponSlots)
        {
            int subSlot = 0;
            foreach (string weapon in item.Value)
            {
                var weaponDef = composer.GetByName(weapon);
                if (weaponDef == null)
                {
                    HelionLog.Warn($"Failed to find weapon {weapon}");
                    continue;
                }

                m_weaponNames.Add(weapon);
                m_weaponSlotLookup[weaponDef.Id] = new(item.Key, subSlot++);
            }
        }
    }

    public WeaponSlot GetWeaponSlot(EntityDefinition def)
    {
        if (m_weaponSlotLookup.TryGetValue(def.Id, out var slot))
            return slot;

        return DefaultSlot;
    }

    public IList<string> GetWeaponDefinitionNames() => m_weaponNames;

    public List<string> GetOwnedWeaponNames()
    {
        List<string> weapons = [];
        foreach (var weapon in m_ownedWeapons)
            weapons.Add(weapon.Definition.Name);
        return weapons;
    }

    public WeaponSlot GetNextSlot(Player player) => CycleSlot(player, player.WeaponSlot, player.WeaponSubSlot, true);
    public WeaponSlot GetPreviousSlot(Player player) => CycleSlot(player, player.WeaponSlot, player.WeaponSubSlot, false);

    public WeaponSlot GetNextSlot(Player player, int amount)
    {
        WeaponSlot slot = new(player.WeaponSlot, player.WeaponSubSlot);
        if (amount == 0)
            return slot;

        bool direction = amount > 0;
        amount = Math.Abs(amount % m_ownedWeapons.Count);
        while (amount > 0)
        {
            slot = CycleSlot(player, slot.Slot, slot.SubSlot, direction);
            amount--;
        }

        return slot;
    }

    private WeaponSlot CycleSlot(Player player, int slot, int subSlot, bool next)
    {
        int startSlot = slot;
        if (m_ownedWeapons.Count == 0)
           return DefaultSlot;

        subSlot = GetNextSubSlot(slot, subSlot, next);
        if (subSlot != -1)
            return new(slot, subSlot);

        if (next)
            slot = GetSlot(++slot, MinSlot, MaxSlot);
        else
            slot = GetSlot(--slot, MinSlot, MaxSlot);

        while (slot != startSlot)
        {
            if (next)
                subSlot = GetFirstSubSlot(slot);
            else
                subSlot = GetSubSlots(slot) - 1;

            var weapon = GetWeapon(player, slot, subSlot);
            if (weapon != null)
                return new(slot, subSlot);

            if (next)
                slot = GetSlot(++slot, MinSlot, MaxSlot);
            else
                slot = GetSlot(--slot, MinSlot, MaxSlot);
        }

        return DefaultSlot;
    }

    public int GetNextSubSlot(int slot, int subSlot, bool next)
    {
        int find = next ? int.MaxValue : int.MinValue;
        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (!m_weaponSlotLookup.TryGetValue(weapon.Definition.Id, out var weaponSlot))
                continue;

            if (weaponSlot.Slot != slot)
                continue;

            if (next && weaponSlot.SubSlot > subSlot && weaponSlot.SubSlot < find)
                find = weaponSlot.SubSlot;

            if (!next && weaponSlot.SubSlot < subSlot && weaponSlot.SubSlot > find)
                find = weaponSlot.SubSlot;
        }

        if (find == subSlot || find == int.MaxValue || find == int.MinValue)
            return GetFirstSubSlot(slot);
        return find;
    }

    private static int GetSlot(int slot, int min, int max)
    {
        if (slot < min)
            slot = max;
        if (slot > max)
            slot = min;
        return slot;
    }

    public Weapon? Add(EntityDefinition definition, Player owner, EntityManager entityManager,
        FrameStateModel? frameStateModel = null, FrameStateModel? flashStateModel = null)
    {
        if (OwnsWeapon(definition))
            return null;

        var (slot, subSlot) = GetWeaponSlot(definition);

        var weapon = new Weapon(definition, owner, entityManager, frameStateModel, flashStateModel);
        m_ownedWeaponsById.Set(definition.Id, weapon);
        m_ownedWeapons.Add(weapon);
        m_ownedWeapons.Sort(m_weaponSelectionOrderCompare);
        return weapon;
    }

    public void Remove(string name)
    {
        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (!weapon.Definition.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;
            m_ownedWeapons.RemoveAt(i);
            m_ownedWeaponsById.Set(weapon.Definition.Id, null);
            WeaponRemoved?.Invoke(this, weapon);
            break;
        }
    }

    public void Clear()
    {
        m_ownedWeapons.Clear();
        m_ownedWeaponsById.SetAll(null);
        WeaponsCleared?.Invoke(this, EventArgs.Empty);
    }

    public int GetFirstSubSlot(int slot)
    {
        int min = int.MaxValue;
        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (!m_weaponSlotLookup.TryGetValue(weapon.Definition.Id, out var weaponSlot))
                continue;

            if (weaponSlot.Slot != slot)
                continue;

            if (weaponSlot.SubSlot < min)
                min = weaponSlot.SubSlot;
        }

        if (min == int.MaxValue)
            return -1;
        return min;
    }

    public int GetBestSubSlot(int slot)
    {
        int max = int.MinValue;
        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (!m_weaponSlotLookup.TryGetValue(weapon.Definition.Id, out var weaponSlot))
                continue;

            if (weaponSlot.Slot != slot)
                continue;

            if (weaponSlot.SubSlot > max)
                max = weaponSlot.SubSlot;
        }

        if (max == int.MinValue)
            return -1;
        return max;
    }

    public int GetSubSlots(int slot)
    {
        int count = 0;
        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (!m_weaponSlotLookup.TryGetValue(weapon.Definition.Id, out var weaponSlot))
                continue;

            if (weaponSlot.Slot != slot)
                continue;

            count++;
        }

        return count;
    }

    public bool OwnsWeapon(EntityDefinition def)
    {
        return m_ownedWeaponsById.TryGetValue(def.Id, out _);
    }

    public bool OwnsWeapon(string name) => GetWeapon(name) != null;

    public List<Weapon> GetWeaponsInSelectionOrder() => m_ownedWeapons;

    public Weapon? GetWeapon(string name)
    {
        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (weapon.Definition.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return weapon;
        }

        return null;
    }

    public bool HasWeaponSlot(Player player, int slot)
    {
        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (!m_weaponSlotLookup.TryGetValue(weapon.Definition.Id, out var weaponSlot))
                continue;

            if (weaponSlot.Slot == slot)
                return true;
        }

        return false;
    }

    public Weapon? GetWeapon(Player player, int slot, int subslot = -1)
    {
        if (slot == player.WeaponSlot && subslot == -1)
            subslot = GetSlot(player.WeaponSubSlot + 1, 0, GetSubSlots(slot));
        else if (subslot == -1)
            subslot = GetBestSubSlot(slot);

        for (int i = 0; i < m_ownedWeapons.Count; i++)
        {
            var weapon = m_ownedWeapons[i];
            if (!m_weaponSlotLookup.TryGetValue(weapon.Definition.Id, out var weaponSlot))
                continue;

            if (weaponSlot.Slot != slot || weaponSlot.SubSlot != subslot)
                continue;

            return weapon;
        }

        return null;
    }
    private static int WeaponSelectionOrderCompare(Weapon w1, Weapon w2)
    {
        return w1.Definition.Properties.Weapons.SelectionOrder.CompareTo(w2.Definition.Properties.Weapons.SelectionOrder);
    }
}
