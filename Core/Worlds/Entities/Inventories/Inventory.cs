using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Util;
using Helion.Worlds.Entities.Definition;
using Helion.Worlds.Entities.Definition.Composer;

namespace Helion.Worlds.Entities.Inventories
{
    public class Inventory
    {
        public static readonly CIString AmmoClassName = "AMMO";
        public static readonly CIString BackPackBaseClassName = "BACKPACKITEM";
        public static readonly CIString WeaponClassName = "WEAPON";
        public static readonly CIString HealthClassName = "HEALTH";
        public static readonly CIString ArmorClassName = "ARMOR";
        public static readonly CIString BasicArmorBonusClassName = "BASICARMORBONUS";
        public static readonly CIString BasicArmorPickupClassName = "BASICARMORPICKUP";
        public static readonly CIString KeyClassName = "KEY";

        /// <summary>
        /// All of the items owned by the player that are not a special type of
        /// item (ex: weapons, which need more logic).
        /// </summary>
        private readonly Dictionary<CIString, InventoryItem> Items = new Dictionary<CIString, InventoryItem>();
        private readonly List<InventoryItem> Keys = new List<InventoryItem>();

        /// <summary>
        /// All of the weapons owned by the player.
        /// </summary>
        public readonly Weapons Weapons = new Weapons();

        public static CIString GetBaseInventoryName(EntityDefinition definition)
        {
            int index = definition.ParentClassNames.IndexOf(AmmoClassName);
            if (index > 0 && index < definition.ParentClassNames.Count - 1)
                return definition.ParentClassNames[index + 1];

            return definition.Name;
        }

        public bool Add(EntityDefinition definition, int amount)
        {
            if (amount <= 0)
                return false;

            CIString name = GetBaseInventoryName(definition);
            int maxAmount = definition.Properties.Inventory.MaxAmount;
            if (definition.IsType(AmmoClassName) && HasItemOfClass(BackPackBaseClassName) && definition.Properties.Ammo.BackpackMaxAmount > maxAmount)
                maxAmount = definition.Properties.Ammo.BackpackMaxAmount;

            bool isKey = definition.IsType(KeyClassName);

            if (Items.TryGetValue(name, out InventoryItem? item))
            {
                if (isKey || item.Amount >= maxAmount)
                    return false;

                item.Amount += amount;
                if (item.Amount > maxAmount)
                    item.Amount = maxAmount;

                return true;
            }
            else
            {
                InventoryItem inventoryItem = new InventoryItem(definition, isKey ? 1 : amount);
                Items[name] = inventoryItem;

                if (isKey)
                {
                    Keys.Add(inventoryItem);
                    SortKeys();
                }
            }

            return true;
        }

        public void AddBackPackAmmo(EntityDefinitionComposer definitionComposer)
        {
            HashSet<CIString> addedBaseNames = new HashSet<CIString>();
            List<EntityDefinition> ammoDefinitions = GetAmmoTypes(definitionComposer).Where(x => x.Properties.Ammo.BackpackAmount > 0).ToList();
            foreach (EntityDefinition ammo in ammoDefinitions)
            {
                CIString baseName = GetBaseInventoryName(ammo);
                if (addedBaseNames.Contains(baseName))
                    continue;

                Add(ammo, ammo.Properties.Ammo.BackpackAmount);
                addedBaseNames.Add(baseName);
            }
        }

        public void GiveAllAmmo(EntityDefinitionComposer definitionComposer)
        {
            EntityDefinition? backpackDef = definitionComposer.GetByName(BackPackBaseClassName);
            if (backpackDef != null)
                Add(backpackDef, 1);
            List<EntityDefinition> ammoDefinitions = GetAmmoTypes(definitionComposer).ToList();
            foreach (EntityDefinition ammo in ammoDefinitions)
                Add(ammo, Math.Max(ammo.Properties.Ammo.BackpackMaxAmount, ammo.Properties.Inventory.Amount));
        }

        public void GiveAllKeys(EntityDefinitionComposer definitionComposer)
        {
            List<EntityDefinition> keys = definitionComposer.GetEntityDefinitions().Where(x => x.IsType(KeyClassName) && x.EditorId.HasValue).ToList();
            keys.ForEach(x => Add(x, 1));
        }

        public void ClearKeys()
        {
            Keys.ForEach(x => Items.Remove(x.Definition.Name));
            Keys.Clear();
        }

        public void Clear()
        {
            Items.Clear();
            Keys.Clear();
        }

        public bool HasItem(CIString name) => Items.ContainsKey(name);

        public bool HasAnyItem(IEnumerable<CIString> names) => names.Any(x => HasItem(x));

        public bool HasItemOfClass(CIString name) => Items.Any(x => x.Value.Definition.IsType(name));

        public int Amount(CIString name) => Items.TryGetValue(name, out var item) ? item.Amount : 0;

        public void Remove(CIString name, int amount)
        {
            if (amount <= 0)
                return;

            if (Items.TryGetValue(name, out InventoryItem? item))
            {
                if (amount < item.Amount)
                    item.Amount -= amount;
                else
                    Items.Remove(name);

                if (item.Definition.IsType(KeyClassName))
                {
                    Keys.Remove(item);
                    SortKeys();
                }

                return;
            }

            // If we didn't find it, then it's possibly indexed in some other
            // data structure (ex: weapons).
            Weapons.Remove(name);
        }

        public List<InventoryItem> GetInventoryItems() => Items.Values.ToList();
        public List<InventoryItem> GetKeys() => Keys;

        public void RemoveAll(CIString name)
        {
            Items.Remove(name);
        }

        private static IEnumerable<EntityDefinition> GetAmmoTypes(EntityDefinitionComposer definitionComposer)
        {
            return definitionComposer.GetEntityDefinitions().Where(x => x.IsType(AmmoClassName));
        }

        private void SortKeys()
        {
            Keys.Sort((i1, i2) => i1.Definition.EditorId.Value.CompareTo(i2.Definition.EditorId.Value));
        }
    }
}