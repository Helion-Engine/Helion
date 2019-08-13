using System;
using System.IO;
using System.Text;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Archives
{
    /// <summary>
    /// A wad specific archive implementation.
    /// </summary>
    public class Wad : Archive, IDisposable
    {
        private const int LumpTableEntryBytes = 16;

        public WadHeader Header;
        private readonly ByteReader m_byteReader;

        public Wad(IEntryPath path) : base(path)
        {
            m_byteReader = new ByteReader(new BinaryReader(File.Open(Path.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)));
            LoadWadEntries();
        }

        public byte[] ReadData(WadEntry entry)
        {
            Precondition(entry.Parent == this, "Bad entry parent");
            
            m_byteReader.Offset(entry.Offset);
            return m_byteReader.ReadBytes(entry.Size);
        }

        public void Dispose()
        {
            m_byteReader.Close();
            m_byteReader.Dispose();
        }

        private WadHeader ReadHeader()
        {
            m_byteReader.Offset(0);
            
            string headerId = Encoding.UTF8.GetString(m_byteReader.ReadBytes(4));
            bool isIwad = (headerId[0] == 'I');
            int numEntries = m_byteReader.ReadInt32();
            int entryTableOffset = m_byteReader.ReadInt32();
            
            return new WadHeader(isIwad, numEntries, entryTableOffset);
        }

        private (int offset, int size, string name) ReadDirectoryEntry()
        {
            int offset = m_byteReader.ReadInt32();
            int size = m_byteReader.ReadInt32();
            string name = m_byteReader.ReadEightByteString().ToUpper();
            return (offset, size, name);
        }

        private void LoadWadEntries()
        {
            WadNamespaceTracker namespaceTracker = new WadNamespaceTracker();
            
            Header = ReadHeader();

            if (Header.DirectoryTableOffset + (Header.EntryCount * LumpTableEntryBytes) > m_byteReader.Length)
                throw new HelionException("Lump entry table runs out of data");

            m_byteReader.Offset(Header.DirectoryTableOffset);
            for (int i = 0; i < Header.EntryCount; i++)
            {
                (int offset, int size, string upperName) = ReadDirectoryEntry();

                // It appears that some markers have an offset of zero, so
                // it is an acceptable offset (we can't check < 12).
                if (offset < 0)
                    throw new HelionException("Lump entry data location underflows");
                if (offset + size > m_byteReader.Length)
                    throw new HelionException("Lump entry data location overflows");

                namespaceTracker.UpdateIfNeeded(upperName);
                WadEntryPath entryPath = new WadEntryPath(upperName);
                Entries.Add(new WadEntry(this, offset, size, entryPath, namespaceTracker.Current));
            }
        }
    }
}