namespace Helion.World.Entities.Definition.Properties.Components
{
    public class DropItemProperty
    {
        public string ClassName;
        public byte Probability;
        public int Amount;

        public DropItemProperty(string className, byte probability = 255, int amount = 1)
        {
            ClassName = className;
            Probability = probability;
            Amount = amount;
        }
    }
}