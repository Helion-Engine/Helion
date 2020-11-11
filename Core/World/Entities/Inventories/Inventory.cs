using System.Collections.Generic;
using Helion.Util;
using Helion.World.Entities.Definition;

namespace Helion.World.Entities.Inventories
{
    public class Inventory
    {
        /// <summary>
        /// All of the items owned by the player that are not a special type of
        /// item (ex: weapons, which need more logic).
        /// </summary>
        private readonly Dictionary<CIString, InventoryItem> Items = new Dictionary<CIString, InventoryItem>();
        
        /// <summary>
        /// All of the weapons owned by the player.
        /// </summary>
        public readonly Weapons Weapons = new Weapons();

        public bool Add(EntityDefinition definition, int amount)
        {
            if (amount <= 0)
                return false;

            if (Items.TryGetValue(definition.Name, out InventoryItem? item))
                item.Amount += amount;
            else
                Items[definition.Name] = new InventoryItem(definition, amount);

            return true;
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(CIString name) => Items.ContainsKey(name);
        
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
                return;
            }

            // If we didn't find it, then it's possibly indexed in some other
            // data structure (ex: weapons).
            Weapons.Remove(name);
        }

        public void RemoveAll(CIString name)
        {
            Items.Remove(name);
        }
    }
}