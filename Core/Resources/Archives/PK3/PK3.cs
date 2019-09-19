using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Archives
{
    /// <summary>
    /// A PK3 specific archive implementation.
    /// </summary>
    public class PK3 : Archive, IDisposable
    {
        private static readonly string DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar.ToString();
        private static readonly string AltDirectorySeparatorChar = System.IO.Path.AltDirectorySeparatorChar.ToString();
        private static readonly Dictionary<CIString, ResourceNamespace> FolderToNamespace = new Dictionary<CIString, ResourceNamespace>()
        {
            ["ACS"] = ResourceNamespace.ACS,
            ["FLATS"] = ResourceNamespace.Flats,
            ["FONTS"] = ResourceNamespace.Fonts,
            ["GRAPHICS"] = ResourceNamespace.Graphics,
            ["HIRES"] = ResourceNamespace.Textures,
            ["MUSIC"] = ResourceNamespace.Music,
            ["SOUNDS"] = ResourceNamespace.Sounds,
            ["SPRITES"] = ResourceNamespace.Sprites,
            ["TEXTURES"] = ResourceNamespace.Textures,
        };

        private readonly ZipArchive m_zipArchive;

        public PK3(IEntryPath path) : base(path)
        {
            m_zipArchive = new ZipArchive(File.Open(Path.FullPath, FileMode.Open));
            Pk3EntriesFromData();
        }

        public void Dispose()
        {
            m_zipArchive.Dispose();
        }

        public byte[] ReadData(PK3Entry entry)
        {
            Invariant(entry.Parent == this, "Bad entry parent");
            
            using (var stream = entry.ZipEntry.Open())
            {
                byte[] data = new byte[entry.ZipEntry.Length];
                stream.Read(data, 0, (int)entry.ZipEntry.Length);
                return data;
            }
        }

        private static bool ZipEntryDirectory(ZipArchiveEntry entry)
        {
            bool isSeparator = entry.FullName.EndsWith(DirectorySeparatorChar) || 
                               entry.FullName.EndsWith(AltDirectorySeparatorChar);
            return entry.Length == 0 && isSeparator;
        }

        private void ZipDataToEntry(ZipArchiveEntry zipEntry)
        {
            if (ZipEntryDirectory(zipEntry))
                return;

            // Windows generates paths with its backward slashes, so we have
            // to handle this. A big problem with this however is that any
            // sprites that use the backslash as part of the sprite name will
            // get toasted by this.
            string forwardSlashPath = zipEntry.FullName.Replace('\\', '/');
            
            EntryPath entryPath = new EntryPath(forwardSlashPath);
            ResourceNamespace resourceNamespace = NamespaceFromEntryPath(forwardSlashPath);
            Entries.Add(new PK3Entry(this, zipEntry, entryPath, resourceNamespace));
        }

        private ResourceNamespace NamespaceFromEntryPath(string forwardSlashPath)
        {
            string[] tokens = forwardSlashPath.Split('/');
            if (tokens.Length > 0)
                if (FolderToNamespace.TryGetValue(tokens[0], out ResourceNamespace resourceNamespace))
                    return resourceNamespace;
            
            return ResourceNamespace.Global;
        }

        private void Pk3EntriesFromData()
        {
            // TODO: we need a way to handle wad entries in a pk3
            try
            {
                m_zipArchive.Entries.ForEach(ZipDataToEntry);
            }
            catch (Exception e)
            {
                throw new Exception($"Unexpected error when reading PK3: {e.Message}");
            }
        }
    }
}