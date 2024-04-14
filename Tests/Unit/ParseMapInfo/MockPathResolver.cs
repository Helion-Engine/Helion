using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.MapInfo;

namespace Helion.Tests.Unit.ParseMapInfo;

internal class MockPathResolver : IPathResolver
{
    public Entry? FindEntryByPath(string path)
    {
        return null;
    }
}
