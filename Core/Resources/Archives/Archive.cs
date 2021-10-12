using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Models;
using Helion.Resources.Archives.Entries;
using Helion.Resources.IWad;

namespace Helion.Resources.Archives;

/// <summary>
/// A container of entries. Intended to be the representation of a wad/pk3.
/// </summary>
public abstract class Archive
{
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
}
