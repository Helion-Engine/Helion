namespace Helion.Util.RandomGenerators
{
    public class NoRandom : IRandom
    {
        public byte NextByte() => 0;
        public int NextDiff() => 0;
        public object Clone() => new NoRandom();
    }
}
