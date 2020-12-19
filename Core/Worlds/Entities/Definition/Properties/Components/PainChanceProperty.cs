namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public readonly struct PainChanceProperty
    {
        public readonly string? Type;
        public readonly double Value;

        public PainChanceProperty(double value)
        {
            Type = null;
            Value = value;
        }

        public PainChanceProperty(string type, double value)
        {
            Type = type;
            Value = value;
        }
    }
}