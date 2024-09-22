using Helion.Resources.Archives.Entries;
using System;

namespace Helion.Resources.Definitions.Id24;

public static class ParseUtil
{
    public static string GetParseError(Entry entry, string definitionName, Exception? ex = null)
    {
        var error = $"Failed to parse {definitionName} from {entry.Parent.Path.Name}";
        if (ex == null)
            return error;

        return $"{error}: {ex.Message}";
    }
}
