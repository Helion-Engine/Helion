using Helion.Resources.Archives.Entries;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Id24;

public class Id24SkyDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public SkyDefinitionData Data { get; set; } = new();
    public Dictionary<string, string> FlatMapping = [];

    public void Parse(Entry entry)
    {
        string data = entry.ReadDataAsString();
        try
        {
            var converted = JsonConvert.DeserializeObject<SkyDefinitions>(data);
            if (converted == null)
            {
                Log.Error(GetParseError(entry));
                return;
            }

            Data = converted.Data;

            foreach (var item in Data.FlatMapping)
                FlatMapping[item.Flat] = item.Sky;

            foreach (var sky in Data.Skies)
            {
                if (!sky.Validate(out string error))
                    Log.Error(error);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, GetParseError(entry));
        }
    }

    private static string GetParseError(Entry entry) => $"Failed to parse skydefs from {entry.Parent.Path.Name}";
}