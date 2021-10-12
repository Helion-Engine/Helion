namespace Helion.Resources.Archives.Entries;

public class WadEntryPath : IEntryPath
{
    public string FullPath { get; }
    public string Name => FullPath;
    public string Extension => string.Empty;
    public string NameWithExtension => Name;
    public bool HasExtension => false;
    public bool IsDirectory => false;

    public WadEntryPath(string path)
    {
        FullPath = path;
    }
}

