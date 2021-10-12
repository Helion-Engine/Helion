using System.Collections.Generic;

namespace Helion.Util.CommandLine;

public record CommandArg
{
    public string Key { get; }
    public List<string> Values { get; } = new();

    public CommandArg(string key)
    {
        Key = key;
    }
}
