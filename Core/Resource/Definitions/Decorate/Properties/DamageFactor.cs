namespace Helion.Resource.Definitions.Decorate.Properties
{
    public readonly struct DamageFactor
    {
        public readonly string? Type;
        public readonly double Value;

        public DamageFactor(double value)
        {
            Type = null;
            Value = value;
        }
        
        public DamageFactor(string type, double value)
        {
            Type = type;
            Value = value;
        }
    }
}