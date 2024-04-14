using Helion.Resources.Archives.Entries;

namespace Helion.Resources.Definitions.MapInfo;

public interface IPathResolver
{
    public Entry? FindEntryByPath(string path);
}
