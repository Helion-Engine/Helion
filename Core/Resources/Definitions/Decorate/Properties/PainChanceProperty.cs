namespace Helion.Resources.Definitions.Decorate.Properties
{
    public readonly struct PainChance
    {
        public readonly string? Type;
        public readonly double Value;

        public PainChance(double value)
        {
            Type = null;
            Value = value;
        }
        
        public PainChance(string type, double value)
        {
            Type = type;
            Value = value;
        }
    }
}