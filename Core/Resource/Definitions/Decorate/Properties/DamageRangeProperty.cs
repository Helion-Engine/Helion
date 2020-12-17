namespace Helion.Resource.Definitions.Decorate.Properties
{
    public struct DamageRangeProperty
    {
        public int? Value;
        public bool? Exact;

        public DamageRangeProperty(int value, bool exact)
        {
            Value = value;
            Exact = exact;
        }
    }
}