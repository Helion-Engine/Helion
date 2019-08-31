namespace Helion.Resources.Definitions.Decorate.Properties
{
    public readonly struct DamageFactor
    {
        public readonly string Type;
        public readonly double Value;

        public DamageFactor(string type, double value)
        {
            Type = type;
            Value = value;
        }
    }
}