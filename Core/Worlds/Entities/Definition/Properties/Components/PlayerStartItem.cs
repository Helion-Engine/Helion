namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public class PlayerStartItem
    {
        public readonly string Name;
        public readonly int Amount;

        public PlayerStartItem(string name, int amount)
        {
            Name = name;
            Amount = amount;
        }
    }
}