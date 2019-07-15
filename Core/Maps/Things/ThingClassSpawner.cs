namespace Helion.Maps.Things
{
    public struct ThingClassSpawner
    {
        public readonly bool Fighter;
        public readonly bool Cleric;
        public readonly bool Mage;

        public ThingClassSpawner(ushort flags)
        {
            Fighter = (flags & (ushort)ThingFlag.Fighter) != 0;
            Cleric = (flags & (ushort)ThingFlag.Cleric) != 0;
            Mage = (flags & (ushort)ThingFlag.Mage) != 0;
        }
    }
}