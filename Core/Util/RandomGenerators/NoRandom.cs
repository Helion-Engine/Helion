namespace Helion.Util.RandomGenerators
{
    public class NoRandom : IRandom
    {
        public int RandomValue { get; set; }

        public int NextByte() => RandomValue;
        public int NextDiff() => RandomValue;
        public int RandomIndex => RandomValue;

        public IRandom Clone() => new NoRandom();
        public IRandom Clone(int randomIndex) => new NoRandom();
    }
}
