namespace Helion.Maps.Things
{
    public struct ThingTranslucency
    {
        public readonly bool QuarterTransparent;
        public readonly bool FullyTransparent;

        public ThingTranslucency(ushort flags)
        {
            QuarterTransparent = (flags & (ushort)ThingFlag.SlightlyTranslucent) != 0;
            FullyTransparent = (flags & (ushort)ThingFlag.FullyTranslucent) != 0;
        }
    }
}