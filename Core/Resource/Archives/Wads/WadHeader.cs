namespace Helion.Resource.Archives.Wads
{
    /// <summary>
    /// A header with wad data.
    /// </summary>
    public record WadHeader
    {
        public readonly bool Iwad;
        public readonly int EntryCount;
        public readonly int TableOffset;

        public WadHeader(bool iwad, int entryCount, int tableOffset)
        {
            Iwad = iwad;
            EntryCount = entryCount;
            TableOffset = tableOffset;
        }
    }
}
