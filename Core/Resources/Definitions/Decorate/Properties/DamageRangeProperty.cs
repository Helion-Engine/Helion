namespace Helion.Resources.Definitions.Decorate.Properties
{
    public struct DamageRangeProperty
    {
        public int Low;
        public int High;
        public bool Exact;

        public int Value => High;

        public DamageRangeProperty(int value) : this(value, value)
        {
            Exact = true;
        }
        
        public DamageRangeProperty(int low, int high)
        {
            Low = low;
            High = high;
            Exact = false;
        }
    }
}