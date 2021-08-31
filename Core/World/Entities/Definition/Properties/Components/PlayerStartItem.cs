namespace Helion.World.Entities.Definition.Properties.Components
{
    public class PlayerStartItem
    {
        public readonly string Name;
        public int Amount { get; set; }

        public PlayerStartItem(string name, int amount)
        {
            Name = name;
            Amount = amount;
        }
    }
}