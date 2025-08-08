using System.IO;

namespace Helion.Resources.Archives.Entries;

/// <summary>
/// Represents a path for an entry inside of an archive.
/// </summary>
public class EntryPath : IEntryPath
{
    public string FullPath { get; }
    public string Name { get; private set; }
    public string Extension { get; }
    public string NameWithExtension => HasExtension ? $"{Name}.{Extension}" : Name;
    public bool HasExtension => !string.IsNullOrEmpty(Extension);
    public bool IsDirectory => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Extension);

    public static EntryPath CreatePathedEntry(string path, ResourceNamespace ns)
    {
        var entry = new EntryPath(path);
        if (ns == ResourceNamespace.Sprites)
            entry.SetName(entry.Name.Replace('^', '\\'));

        return entry;
    }

    public EntryPath(string path = "")
    {
        FullPath = CleanPath(path);
        Name = Path.GetFileNameWithoutExtension(FullPath);
        Extension = Path.GetExtension(FullPath);

        if (Extension.Length > 1)
            Extension = Extension.Substring(1);
    }

    public EntryPath(string fullPath, string name)
    {
        FullPath = CleanPath(fullPath);
        Name = Path.GetFileNameWithoutExtension(name);
        Extension = Path.GetExtension(name);

        if (Extension.Length > 1)
            Extension = Extension.Substring(1);
    }

    public void SetName(string name)
    {
        Name = name;
    }

    public override string ToString() => FullPath;

    private static string CleanPath(string path)
    {
        // Windows generates paths with its backward slashes, so we have
        // to handle this. A big problem with this however is that any
        // sprites that use the backslash as part of the sprite name will
        // get toasted by this.
        return path.Replace('\\', '/');
    }
}
