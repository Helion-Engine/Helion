namespace Helion.Util.RandomGenerators
{
    public class NoRandom : IRandom
    {
        public byte NextByte() => 0;
        public int NextDiff() => 0;
        public int RandomIndex => 0;

        public IRandom Clone() => new NoRandom();
        public IRandom Clone(int randomIndex) => new NoRandom();
    }
}
