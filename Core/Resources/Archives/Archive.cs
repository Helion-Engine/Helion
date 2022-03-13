using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Models;
using Helion.Resources.Archives.Entries;
using Helion.Resources.IWad;
using Helion.Util.Extensions;

namespace Helion.Resources.Archives;

/// <summary>
/// A container of entries. Intended to be the representation of a wad/pk3.
/// </summary>
public abstract class Archive : IDisposable
{
    protected static readonly char DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
    protected static readonly char AltDirectorySeparatorChar = System.IO.Path.AltDirectorySeparatorChar;
    protected static readonly char[] DirectorySeparatorChars = new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

    protected static readonly (string, ResourceNamespace)[] FolderToNamespace = new (string, ResourceNamespace)[]
    {
        ("ACS", ResourceNamespace.ACS),
        ("FLATS", ResourceNamespace.Flats),
        ("FONTS", ResourceNamespace.Fonts),
        ("GRAPHICS", ResourceNamespace.Graphics),
        ("HIRES", ResourceNamespace.Textures),
        ("MUSIC", ResourceNamespace.Music),
        ("SOUNDS", ResourceNamespace.Sounds),
        ("SPRITES", ResourceNamespace.Sprites),
        ("TEXTURES", ResourceNamespace.Textures),
        ("PATCHES", ResourceNamespace.Textures)
    };

    protected static ResourceNamespace NamespaceFromEntryPath(string path)
    {
        if (!path.GetLastFolder(out var folder))
            return ResourceNamespace.Global;

        foreach (var item in FolderToNamespace)
        {
            if (folder.Equals(item.Item1, StringComparison.OrdinalIgnoreCase))
                return item.Item2;
        }

        return ResourceNamespace.Global;
    }

    protected static string CleanPath(string path)
    {
        // Windows generates paths with its backward slashes, so we have
        // to handle this. A big problem with this however is that any
        // sprites that use the backslash as part of the sprite name will
        // get toasted by this.
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// All the entries in this archive.
    /// </summary>
    public List<Entry> Entries { get; } = new List<Entry>();

    /// <summary>
    /// The path to this entry. This will be an empty path if it's the root
    /// in a hierarchy.
    /// </summary>
    public readonly IEntryPath Path;

    /// <summary>
    /// The hash of the archive.
    /// </summary>
    // TODO: Implement!
    public string MD5 = "00000000000000000000000000000000";

    public ArchiveType ArchiveType { get; set; }
    public IWadInfo IWadInfo { get; set; } = IWadInfo.DefaultIWadInfo;
    public Archive? ExtractedFrom { get; set; }

    public string OriginalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Creates an archive, which may either be a top level file or some
    /// nested entry.
    /// </summary>
    /// <param name="path">The path to this entry.</param>
    protected Archive(IEntryPath path)
    {
        Path = path;
    }

    public FileModel ToFileModel()
    {
        return new FileModel()
        {
            FileName = System.IO.Path.GetFileName(OriginalFilePath),
            MD5 = MD5
        };
    }

    public Entry? GetEntryByName(string name) => Entries.FirstOrDefault(x => x.Path.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public bool AnyEntryByName(string name) => Entries.Any(x => x.Path.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public abstract void Dispose();
}
