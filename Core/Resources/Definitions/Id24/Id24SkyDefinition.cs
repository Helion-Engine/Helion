using Helion.Resources.Archives.Entries;
using Helion.Util;
using NLog;
using System;
using System.Collections.Generic;
using System.Text.Json;

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
            var converted = JsonSerializer.Deserialize<SkyDefinitions>(data, JsonSerializationSettings.IgnoreNull) ?? throw new Exception("Data was null");
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
            Log.Error(ex, ParseUtil.GetParseError(entry, "skydefs", ex));
        }
    }
}