using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Helion.Resources;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Entries.Archive
{
    /// <summary>
    /// A PK3 specific archive implementation.
    /// </summary>
    public class Pk3 : Archive
    {
        private static readonly Dictionary<CiString, ResourceNamespace> FOLDER_TO_NAMESPACE = new Dictionary<CiString, ResourceNamespace>()
        {
            ["ACS"] = ResourceNamespace.ACS,
            ["FLATS"] = ResourceNamespace.Flats,
            ["FONTS"] = ResourceNamespace.Fonts,
            ["MUSIC"] = ResourceNamespace.Music,
            ["SOUNDS"] = ResourceNamespace.Sounds,
            ["SPRITES"] = ResourceNamespace.Sprites,
            ["TEXTURES"] = ResourceNamespace.Textures
        };

        private ZipArchive m_zipArchive;

        public Pk3(IEntryPath path) :
            base(path)
        {
            m_zipArchive = new ZipArchive(File.Open(Path.FullPath, FileMode.Open));
            Pk3EntriesFromData();
        }

        public void Dispose()
        {
            m_zipArchive.Dispose();
        }

        public byte[] ReadData(Pk3Entry entry)
        {
            Postcondition(entry.Parent == this, "Bad entry parent");
            using (var stream = entry.ZipeEntry.Open())
            {
                byte[] data = new byte[entry.ZipeEntry.Length];
                stream.Read(data, 0, (int)entry.ZipeEntry.Length);
                return data;
            }
        }

        private static bool ZipEntryDirectory(ZipArchiveEntry entry)
        {
            return entry.Length == 0 && (entry.FullName.EndsWith(System.IO.Path.DirectorySeparatorChar) || entry.FullName.EndsWith(System.IO.Path.AltDirectorySeparatorChar));
        }

        private void ZipDataToEntry(ZipArchiveEntry zipEntry)
        {
            if (ZipEntryDirectory(zipEntry))
                return;

            EntryPath entryPath = new EntryPath(zipEntry.FullName);
            Entries.Add(new Pk3Entry(this, zipEntry, entryPath, ResourceNamespace.Global));
        }

        private void Pk3EntriesFromData()
        {
            //TODO we need a way to handle wad entries in a pk3
            try
            {
                foreach (ZipArchiveEntry entry in m_zipArchive.Entries)
                    ZipDataToEntry(entry);
            }
            catch (Exception e)
            {
                throw new Exception($"Unexpected error when reading PK3: {e.Message}");
            }
        }

        public override ArchiveType GetArchiveType() => ArchiveType.Pk3;
    }
}