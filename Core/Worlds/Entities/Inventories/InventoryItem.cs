using Helion.Worlds.Entities.Definition;
using static Helion.Util.Assertion.Assert;

namespace Helion.Worlds.Entities.Inventories
{
    public class InventoryItem
    {
        public readonly EntityDefinition Definition;
        public int Amount;

        public InventoryItem(EntityDefinition definition, int amount)
        {
            Precondition(amount > 0, "Should not be giving a negative or zero amount of an inventory item");

            Definition = definition;
            Amount = amount;
        }
    }
}