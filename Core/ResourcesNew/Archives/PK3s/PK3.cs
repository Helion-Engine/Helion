using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Helion.Util;
using MoreLinq.Extensions;

namespace Helion.ResourcesNew.Archives.PK3s
{
    public class PK3 : Archive
    {
        private static readonly string DirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
        private static readonly string AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar.ToString();
        private static readonly Dictionary<CIString, Namespace> FolderToNamespace = new()
        {
            ["ACS"] = Namespace.ACS,
            ["FLATS"] = Namespace.Flats,
            ["FONTS"] = Namespace.Fonts,
            ["GRAPHICS"] = Namespace.Graphics,
            ["HIRES"] = Namespace.Textures,
            ["MUSIC"] = Namespace.Music,
            ["SOUNDS"] = Namespace.Sounds,
            ["SPRITES"] = Namespace.Sprites,
            ["TEXTURES"] = Namespace.Textures,
        };

        private readonly FileStream m_fileStream;
        private readonly ZipArchive m_zipArchive;

        public PK3(string md5, FileStream fileStream) : base(md5)
        {
            m_fileStream = fileStream;
            m_zipArchive = new(fileStream);

            Pk3EntriesFromData();
        }

        public static PK3? FromFile(string path)
        {
            string? md5 = Files.CalculateMD5(path);
            if (md5 == null)
                return null;

            try
            {
                FileStream fileStream = File.Open(path, FileMode.Open);
                return new PK3(md5, fileStream);
            }
            catch
            {
                return null;
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
            EntryPath entryPath = new(forwardSlashPath);
            Namespace resourceNamespace = NamespaceFromEntryPath(forwardSlashPath);

            PK3Entry pk3Entry = new(zipEntry, entryPath, resourceNamespace);
            AddEntry(pk3Entry);
        }

        private Namespace NamespaceFromEntryPath(string forwardSlashPath)
        {
            string[] tokens = forwardSlashPath.Split('/');
            if (tokens.Length > 0)
                if (FolderToNamespace.TryGetValue(tokens[0], out Namespace resourceNamespace))
                    return resourceNamespace;

            return Namespace.Global;
        }

        private void Pk3EntriesFromData()
        {
            try
            {
                // TODO: we need a way to handle wad entries in a pk3.
                m_zipArchive.Entries.ForEach(ZipDataToEntry);
            }
            catch (Exception e)
            {
                throw new Exception($"Unexpected error when reading PK3: {e.Message}");
            }
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            base.Dispose();

            m_zipArchive.Dispose();
            m_fileStream.Dispose();
        }
    }
}
