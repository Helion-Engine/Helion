using Helion.Resources;
using Helion.Util;
using System;
using System.IO;
using System.Text;

namespace Helion.Entries.Archive
{
    /// <summary>
    /// A wad specific archive implementation.
    /// </summary>
    public class Wad : Archive
    {
        private const int LumpTableEntryBytes = 16;
        private ByteReader m_byteReader;

        public WadType WadType { get; private set; }

        public Wad(IEntryPath path) :
            base(path)
        {
            m_byteReader = new ByteReader(new BinaryReader(File.Open(Path.FullPath, FileMode.Open)));
            LoadWadEntries();
        }

        public void Dispose()
        {
            m_byteReader.Close();
            m_byteReader.Dispose();
        }

        private static WadType WadTypeFrom(string header)
        {
            switch (header)
            {
            case "IWAD":
                return WadType.Iwad;
            case "PWAD":
                return WadType.Pwad;
            default:
                return WadType.Unknown;
            }
        }

        private Tuple<WadType, int, int> ReadHeader()
        {
            m_byteReader.Offset(0);
            WadType wadType = WadTypeFrom(Encoding.UTF8.GetString(m_byteReader.ReadBytes(4)));
            int numEntries = m_byteReader.ReadInt32();
            int entryTableOffset = m_byteReader.ReadInt32();
            return Tuple.Create(wadType, numEntries, entryTableOffset);
        }

        private Tuple<int, int, string> ReadDirectoryEntry()
        {
            int offset = m_byteReader.ReadInt32();
            int size = m_byteReader.ReadInt32();
            string name = m_byteReader.ReadEightByteString().ToUpper();
            return Tuple.Create(offset, size, name);
        }

        private void LoadWadEntries()
        {
            (WadType wadType, int numEntries, int entryTableOffset) = ReadHeader();
            WadType = wadType;

            if (wadType == WadType.Unknown)
                throw new Exception("Wad header is corrupt");
            if (entryTableOffset + (numEntries * LumpTableEntryBytes) > m_byteReader.Length)
                throw new Exception("Lump entry table runs out of data");

            for (int i = 0; i < numEntries; i++)
            {
                m_byteReader.Offset(entryTableOffset);
                entryTableOffset += LumpTableEntryBytes;
                (int offset, int size, string upperName) = ReadDirectoryEntry();

                // It appears that some markers have an offset of zero, so
                // it is an acceptable offset (we can't check < 12).
                if (offset < 0)
                    throw new Exception("Lump entry data location underflows");
                if (offset + size > m_byteReader.Length)
                    throw new Exception("Lump entry data location overflows");

                WadEntryPath entryPath = new WadEntryPath(upperName);
                Entries.Add(new WadEntry(this, offset, size, entryPath, ResourceNamespace.Global));
            }
        }

        public byte[] ReadData(WadEntry entry)
        {
            Assert.Precondition(entry.Parent == this, "Bad entry parent");
            m_byteReader.Offset(entry.Offset);
            return m_byteReader.ReadBytes(entry.Size);
        }

        public override ArchiveType GetArchiveType() => ArchiveType.Wad;
    }
}