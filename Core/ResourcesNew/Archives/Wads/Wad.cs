using System.Collections.Generic;
using System.Text;
using Helion.Util;
using ByteReader = Helion.Util.Bytes.ByteReader;

namespace Helion.ResourcesNew.Archives.Wads
{
    public abstract class Wad : Archive
    {
        protected Wad(string md5) : base(md5)
        {
        }

        protected void ReadEntriesOrThrow(ByteReader reader)
        {
            WadHeader header = ReadHeader(reader);
            IEnumerable<DirectoryEntry> directoryEntries = ReadEntryTable(reader, header);
            PopulateEntries(directoryEntries);
        }

        protected internal abstract byte[] ReadEntryData(DirectoryEntry dirEntry);

        private static IEnumerable<DirectoryEntry> ReadEntryTable(ByteReader reader, WadHeader header)
        {
            WadNamespaceTracker namespaceTracker = new();
            List<DirectoryEntry> directoryEntries = new();

            reader.Position = header.TableOffset;
            for (int i = 0; i < header.EntryCount; i++)
            {
                int offset = reader.Int();
                int size = reader.Int();
                CIString name = reader.EightByteString().ToUpper();
                Namespace resourceNamespace = namespaceTracker.Update(name);

                DirectoryEntry dirEntry = new(offset, size, name, resourceNamespace);
                directoryEntries.Add(dirEntry);
            }

            return directoryEntries;
        }

        private void PopulateEntries(IEnumerable<DirectoryEntry> directoryEntries)
        {
            foreach (DirectoryEntry dirEntry in directoryEntries)
            {
                LumpEntry lumpEntry = new(this, dirEntry);
                AddEntry(lumpEntry);
            }
        }

        private static WadHeader ReadHeader(ByteReader reader)
        {
            string headerId = Encoding.UTF8.GetString(reader.Bytes(4));
            bool isIwad = (headerId[0] == 'I');
            int numEntries = reader.Int();
            int entryTableOffset = reader.Int();

            return new WadHeader(isIwad, numEntries, entryTableOffset);
        }
    }
}
