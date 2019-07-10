namespace Helion.Entries
{
    class WadEntryPath : IEntryPath
    {
        public string FullPath { get; protected set; }
        public string Name => FullPath;
        public string Extension => string.Empty;
        public bool HasExtension => false;
        public bool IsDirectory => false;

        public WadEntryPath(string path)
        {
            FullPath = path;
        }
    }
}