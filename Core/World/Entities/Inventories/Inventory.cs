using System.Collections.Generic;
using Helion.Util;
using Helion.World.Entities.Definition;

namespace Helion.World.Entities.Inventories
{
    public class Inventory
    {
        public readonly Dictionary<CIString, InventoryItem> Items = new Dictionary<CIString, InventoryItem>();

        public void Add(EntityDefinition definition, int amount)
        {
            if (amount <= 0 || !definition.Flags.InventoryItem)
                return;

            if (Items.TryGetValue(definition.Name, out InventoryItem item))
                item.Amount += amount;
            else
                Items[definition.Name] = new InventoryItem(definition, amount);
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
            
            if (Items.TryGetValue(name, out InventoryItem item))
            {
                if (amount < item.Amount)
                    item.Amount -= amount;
                else
                    Items.Remove(name);
            }
        }

        public void RemoveAll(CIString name)
        {
            Items.Remove(name);
        }
    }
}