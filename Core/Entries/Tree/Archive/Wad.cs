using Helion.Resources;
using Helion.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Helion.Entries.Tree.Archive
{
    /// <summary>
    /// A wad specific archive implementation.
    /// </summary>
    public class Wad : Archive
    {
        private const int LUMP_TABLE_ENTRY_BYTES = 16;

        private static readonly Dictionary<string, ResourceNamespace> ENTRY_TO_MARKER = new Dictionary<string, ResourceNamespace>()
        {
            ["F_START"] = ResourceNamespace.Flats,
            ["P_START"] = ResourceNamespace.Textures,
            ["S_START"] = ResourceNamespace.Sprites,
            ["T_START"] = ResourceNamespace.Textures,
            ["TX_START"] = ResourceNamespace.Textures,
            ["F_END"] = ResourceNamespace.Global,
            ["P_END"] = ResourceNamespace.Global,
            ["S_END"] = ResourceNamespace.Global,
            ["T_END"] = ResourceNamespace.Global,
            ["TX_END"] = ResourceNamespace.Global,
        };

        /// <summary>
        /// A file path that it may have been read from. If this has a value,
        /// then it will contain the location on the hard disk from where it
        /// was read from.
        /// </summary>
        public Optional<string> FilePath { get; } = Optional.Empty;

        /// <summary>
        /// True if this is an iwad, false if it's a pwad.
        /// </summary>
        public bool IsIwad { get; }

        /// <summary>
        /// Is true if this was from a file path, false if it's from a memory
        /// stream or byte array.
        /// </summary>
        public bool IsFile => FilePath.HasValue;

        private Wad(EntryId id, List<Entry> entries, string filePath, bool isIwad, EntryIdAllocator idAllocator) :
            base(id, new EntryPath(System.IO.Path.GetFileName(filePath)))
        {
            FilePath = filePath;
            IsIwad = isIwad;
            entries.ForEach(entry => AddEntry(entry, idAllocator));
        }

        private Wad(EntryId id, List<Entry> entries, EntryPath path, bool isIwad, EntryIdAllocator idAllocator) :
            base(id, path)
        {
            IsIwad = isIwad;
            entries.ForEach(entry => AddEntry(entry, idAllocator));
        }

        private static bool CheckIfIwad(byte[] data) => (data.Length > 0 && data[0] == 'I');

        private static Tuple<int, int> ReadHeader(ByteReader reader)
        {
            reader.ReadInt32(); // Consume remaining IWAD/PWAD letters.
            int numEntries = reader.ReadInt32();
            int entryTableOffset = reader.ReadInt32();
            return Tuple.Create(numEntries, entryTableOffset);
        }

        private static Tuple<int, int, string> ReadDirectoryEntry(ByteReader reader)
        {
            int offset = reader.ReadInt32();
            int size = reader.ReadInt32();
            string name = reader.ReadEightByteString().ToUpper();
            return Tuple.Create(offset, size, name);
        }

        private static void UpdateNamespace(EntryPath path, ref ResourceNamespace currentNamespace)
        {
            // NOTE: This sets the *_START marker to be in the namespace it 
            // defines. It isn't ideal but we don't use these markers at all 
            // so it's not a major priority to fix this currently.
            if (ENTRY_TO_MARKER.TryGetValue(path.Name, out ResourceNamespace newNamespace))
                currentNamespace = newNamespace;
        }

        private static Expected<List<Entry>, string> WadEntriesFromData(byte[] data, EntryClassifier classifier)
        {
            try
            {
                ByteReader reader = new ByteReader(data);
                (int numEntries, int entryTableOffset) = ReadHeader(reader);

                if (entryTableOffset + (numEntries * LUMP_TABLE_ENTRY_BYTES) > data.Length)
                    return "Lump entry table runs out of data";

                List<Entry> entries = new List<Entry>();
                ResourceNamespace currentNamespace = ResourceNamespace.Global;

                for (int i = 0; i < numEntries; i++)
                {
                    reader.Offset(entryTableOffset);
                    entryTableOffset += LUMP_TABLE_ENTRY_BYTES;
                    (int offset, int size, string upperName) = ReadDirectoryEntry(reader);

                    // It appears that some markers have an offset of zero, so
                    // it is an acceptable offset (we can't check < 12).
                    if (offset < 0)
                        return "Lump entry data location underflows";
                    if (offset + size > data.Length)
                        return "Lump entry data location overflows";

                    reader.Offset(offset);
                    byte[] lumpData = reader.ReadBytes(size);

                    EntryPath path = new EntryPath(upperName);
                    UpdateNamespace(path, ref currentNamespace);

                    Entry entry = classifier.ToEntry(path, lumpData, currentNamespace);
                    entries.Add(entry);
                }

                return entries;
            }
            catch (Exception e)
            {
                return $"Wad data corruption: {e.Message}";
            }
        }

        /// <summary>
        /// Reads a wad from the data source provided.
        /// </summary>
        /// <param name="data">The wad data.</param>
        /// <param name="wadPath">The path to this entry.</param>
        /// <param name="idAllocator">The entry ID allocator.</param>
        /// <param name="classifier">The entry classifier.</param>
        /// <returns>The processed wad if it exists and was able to be
        /// catalogued, otherwise an error reason.</returns>
        public static Expected<Wad, string> FromData(byte[] data, EntryPath wadPath,
            EntryIdAllocator idAllocator, EntryClassifier classifier)
        {
            Expected<List<Entry>, string> entries = WadEntriesFromData(data, classifier);
            if (entries)
                return new Wad(idAllocator.AllocateId(), entries.Value, wadPath, CheckIfIwad(data), idAllocator);
            return $"Unable to read wad data: {entries.Error}";
        }

        /// <summary>
        /// Reads a wad from the file path provided.
        /// </summary>
        /// <param name="filePath">The path to the wad file.</param>
        /// <param name="idAllocator">The entry ID allocator.</param>
        /// <param name="classifier">The entry classifier.</param>
        /// <returns>The processed wad file if it exists and was able to be
        /// catalogued, otherwise an error reason.</returns>
        public static Expected<Wad, string> FromFile(string filePath, EntryIdAllocator idAllocator,
            EntryClassifier classifier)
        {
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                Expected<List<Entry>, string> entries = WadEntriesFromData(data, classifier);
                if (entries)
                    return new Wad(idAllocator.AllocateId(), entries.Value, filePath, CheckIfIwad(data), idAllocator);
                return $"Unable to read wad file: {entries.Error}";
            }
            catch (Exception e)
            {
                return $"Unable to read wad file: {e.Message}";
            }
        }

        public override ResourceType GetResourceType() => ResourceType.Wad;
        public override ArchiveType GetArchiveType() => ArchiveType.Wad;
    }
}
