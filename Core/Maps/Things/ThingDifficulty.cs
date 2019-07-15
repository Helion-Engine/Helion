namespace Helion.Maps.Things
{
    public struct ThingDifficulty
    {
        public readonly bool Easy;
        public readonly bool Medium;
        public readonly bool Hard;

        public ThingDifficulty(ushort flags)
        {
            Easy = (flags & (ushort)ThingFlag.Easy) != 0;
            Medium = (flags & (ushort)ThingFlag.Medium) != 0;
            Hard = (flags & (ushort)ThingFlag.Hard) != 0;
        }
    }
}