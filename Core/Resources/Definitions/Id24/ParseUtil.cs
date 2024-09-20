using Helion.Resources.Archives.Entries;

namespace Helion.Resources.Definitions.Id24;

public static class ParseUtil
{
    public static string GetParseError(Entry entry, string definitionName) => $"Failed to parse {definitionName} from {entry.Parent.Path.Name}";
}
