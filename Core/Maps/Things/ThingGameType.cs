namespace Helion.Maps.Things
{
    public struct ThingGameType
    {
        public readonly bool SinglePlayer;
        public readonly bool Cooperative;
        public readonly bool Deathmatch;

        public ThingGameType(ushort flags)
        {
            SinglePlayer = (flags & (ushort)ThingFlag.SinglePlayer) != 0;
            Cooperative = (flags & (ushort)ThingFlag.Cooperative) != 0;
            Deathmatch = (flags & (ushort)ThingFlag.Deathmatch) != 0;
        }
    }
}