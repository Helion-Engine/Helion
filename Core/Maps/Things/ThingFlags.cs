namespace Helion.Maps.Things
{
    public struct ThingFlags
    {
        public readonly DoomThingFlags Doom;
        public readonly ZDoomThingFlags ZDoom;
        
        public ThingFlags(ushort flags)
        {
            Doom = new DoomThingFlags(flags);
            ZDoom = new ZDoomThingFlags(flags);
        }
    }
}