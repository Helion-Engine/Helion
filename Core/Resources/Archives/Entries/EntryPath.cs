using System.IO;

namespace Helion.Resources.Archives.Entries;

/// <summary>
/// Represents a path for an entry inside of an archive.
/// </summary>
public class EntryPath : IEntryPath
{
    public string FullPath { get; }
    public string Name { get; }
    public string Extension { get; }
    public string NameWithExtension => HasExtension ? $"{Name}.{Extension}" : Name;
    public bool HasExtension => !string.IsNullOrEmpty(Extension);
    public bool IsDirectory => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Extension);

    public EntryPath(string path = "")
    {
        FullPath = path;
        Name = Path.GetFileNameWithoutExtension(FullPath);
        Extension = Path.GetExtension(FullPath);

        if (Extension.Length > 1)
            Extension = Extension.Substring(1);
    }

    public override string ToString() => FullPath;
}

