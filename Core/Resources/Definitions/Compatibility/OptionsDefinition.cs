using Helion.Resources.Archives.Entries;
using NLog;
using System;

namespace Helion.Resources.Definitions.Compatibility;

public class OptionsDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Options Data { get; set; } = new();

    public void Parse(Entry entry)
    {
        try
        {
            string text = entry.ReadDataAsString();
            Data = new(text);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to parse options from {entry.Parent.Path.Name}");
        }
    }
}
