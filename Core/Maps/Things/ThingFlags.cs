namespace Helion.Maps.Things
{
    public struct ThingFlags
    {
        public readonly ThingClassSpawner ClassSpawner;
        public readonly ThingDifficulty Difficulty;
        public readonly ThingGameType GameType;
        public readonly ThingTranslucency Translucency;
        public readonly bool Ambush;
        public readonly bool Dormant;
        public readonly bool Friendly;
        public readonly bool StandStill;

        public ThingFlags(ushort flags)
        {
            ClassSpawner = new ThingClassSpawner(flags);
            Difficulty = new ThingDifficulty(flags);
            GameType = new ThingGameType(flags);
            Translucency = new ThingTranslucency(flags);
            Ambush = (flags & (ushort)ThingFlag.Ambush) != 0;
            Dormant = (flags & (ushort)ThingFlag.Dormant) != 0;
            Friendly = (flags & (ushort)ThingFlag.Friendly) != 0;
            StandStill = (flags & (ushort)ThingFlag.StandStill) != 0;
        } 
    }
}