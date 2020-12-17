namespace Helion.Resource.Archives.Wads
{
    public class LumpEntry : Entry
    {
        private readonly DirectoryEntry m_directoryEntry;
        private readonly Wad m_wad;
        private byte[]? m_data;

        public LumpEntry(Wad wad, DirectoryEntry directoryEntry) :
            base(new EntryPath(directoryEntry.Name.ToString()), directoryEntry.Namespace)
        {
            m_directoryEntry = directoryEntry;
            m_wad = wad;
        }

        public override byte[] ReadData() => m_data ??= m_wad.ReadEntryData(m_directoryEntry);
    }
}
