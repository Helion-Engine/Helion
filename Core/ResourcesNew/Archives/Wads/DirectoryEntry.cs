using Helion.Resources;
using Helion.Util;

namespace Helion.ResourcesNew.Archives.Wads
{
    /// <summary>
    /// A directory entry in the wad.
    /// </summary>
    public record DirectoryEntry
    {
        public int Offset { get; init; }
        public int Size { get; init; }
        public CIString Name { get; init; }
        public Namespace Namespace { get; init; }

        public DirectoryEntry(int offset, int size, CIString name, Namespace resourceNamespace)
        {
            Offset = offset;
            Size = size;
            Name = name;
            Namespace = resourceNamespace;
        }
    }
}
