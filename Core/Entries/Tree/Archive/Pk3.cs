using Helion.Resources;
using Helion.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Helion.Entries.Tree.Archive
{
    /// <summary>
    /// A PK3 specific archive implementation.
    /// </summary>
    public class Pk3 : Archive
    {
        private static readonly Dictionary<UpperString, ResourceNamespace> FOLDER_TO_NAMESPACE = new Dictionary<UpperString, ResourceNamespace>()
        {
            ["ACS"] = ResourceNamespace.ACS,
            ["FLATS"] = ResourceNamespace.Flats,
            ["FONTS"] = ResourceNamespace.Fonts,
            ["MUSIC"] = ResourceNamespace.Music,
            ["SOUNDS"] = ResourceNamespace.Sounds,
            ["SPRITES"] = ResourceNamespace.Sprites,
            ["TEXTURES"] = ResourceNamespace.Textures
        };

        /// <summary>
        /// A file path that it may have been read from. If this has a value,
        /// then it will contain the location on the hard disk from where it
        /// was read from.
        /// </summary>
        public Optional<string> FilePath { get; } = Optional.Empty;

        /// <summary>
        /// Is true if this was from a file path, false if it's from a memory
        /// stream or byte array.
        /// </summary>
        public bool IsFile => FilePath.HasValue;

        private Pk3(EntryId id, List<Entry> entries, string filePath, EntryIdAllocator idAllocator) :
            base(id, new EntryPath(System.IO.Path.GetFileName(filePath)))
        {
            FilePath = filePath;
            entries.ForEach(entry => AddEntry(entry, idAllocator));
        }

        private Pk3(EntryId id, List<Entry> entries, EntryPath path, EntryIdAllocator idAllocator) :
            base(id, path)
        {
            entries.ForEach(entry => AddEntry(entry, idAllocator));
        }

        private static ResourceNamespace GetNamespaceFrom(EntryPath path)
        {
            ResourceNamespace entryNamespace = ResourceNamespace.Global;
            path.RootFolder.Then(folder =>
            {
                if (FOLDER_TO_NAMESPACE.TryGetValue(folder, out ResourceNamespace foundNamespace))
                    entryNamespace = foundNamespace;
            });

            return entryNamespace;
        }

        private static bool ZipEntryDirectory(ZipArchiveEntry entry)
        {
            return entry.Length == 0 && (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'));
        }

        private static void ZipDataToEntry(ICollection<Entry> entries, ZipArchiveEntry zipEntry,
            Stream stream, EntryClassifier classifier)
        {
            if (ZipEntryDirectory(zipEntry))
                return;

            byte[] data = new byte[zipEntry.Length];
            stream.Read(data, 0, (int)zipEntry.Length);

            // TODO: We could use entry.Name to avoid the 'valid slashes in name' issue.
            EntryPath entryPath = new EntryPath(zipEntry.FullName);

            ResourceNamespace resourceNamespace = GetNamespaceFrom(entryPath);
            Entry entry = classifier.ToEntry(entryPath, data, resourceNamespace);
            entries.Add(entry);
        }

        private static Expected<List<Entry>, string> Pk3EntriesFromData(byte[] data,
            EntryClassifier classifier)
        {
            List<Entry> entries = new List<Entry>();

            try
            {
                using (ZipArchive zip = new ZipArchive(new MemoryStream(data)))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        using (Stream stream = entry.Open())
                        {
                            ZipDataToEntry(entries, entry, stream, classifier);
                        }
                    }
                }

                return entries;
            }
            catch (Exception e)
            {
                return $"Unexpected error when reading PK3: {e.Message}";
            }
        }

        /// <summary>
        /// Reads a PK3 from the data source provided.
        /// </summary>
        /// <remarks>
        /// This is intended for PK3s that are nested inside of PK3s. While the
        /// act of doing that is a bad practice, it is supported. This means it
        /// should have a path to the entry (since this is a glorified entry
        /// reader in a sense).
        /// </remarks>
        /// <param name="data">The PK3 data.</param>
        /// <param name="path">The path to this entry.</param>
        /// <param name="idAllocator">The entry ID allocator.</param>
        /// <param name="classifier">The entry classifier.</param>
        /// <returns>The processed PK3 data if it exists and was able to be
        /// catalogued, otherwise an error reason.</returns>
        public static Expected<Pk3, string> FromData(byte[] data, EntryPath path,
            EntryIdAllocator idAllocator, EntryClassifier classifier)
        {
            Expected<List<Entry>, string> entries = Pk3EntriesFromData(data, classifier);
            if (entries)
                return new Pk3(idAllocator.AllocateId(), entries.Value, path, idAllocator);
            return $"Unable to read PK3 data: {entries.Error}";
        }

        /// <summary>
        /// Reads a PK3 from the file path provided.
        /// </summary>
        /// <param name="filePath">The path to the PK3 file.</param>
        /// <param name="idAllocator">The entry ID allocator.</param>
        /// <param name="classifier">The entry classifier.</param>
        /// <returns>The processed PK3 file if it exists and was able to be
        /// catalogued, otherwise an error reason.</returns>
        public static Expected<Pk3, string> FromFile(string filePath, EntryIdAllocator idAllocator,
            EntryClassifier classifier)
        {
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                Expected<List<Entry>, string> entries = Pk3EntriesFromData(data, classifier);
                if (entries)
                    return new Pk3(idAllocator.AllocateId(), entries.Value, filePath, idAllocator);
                return $"Unable to read PK3 file: {entries.Error}";
            }
            catch (Exception e)
            {
                return $"Unable to read PK3 file: {e.Message}";
            }
        }

        public override ArchiveType GetArchiveType() => ArchiveType.Pk3;
        public override ResourceType GetResourceType() => ResourceType.Pk3;
    }
}
