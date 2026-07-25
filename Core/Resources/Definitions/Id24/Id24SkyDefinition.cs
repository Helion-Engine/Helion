using Helion.Resources.Archives.Entries;
using NLog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Helion.Util.SerializationContexts;

namespace Helion.Resources.Definitions.Id24;

public class Id24SkyDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public SkyDefinitionData Data { get; set; } = new();
    public Dictionary<string, string> FlatMapping = [];

    public void Parse(Entry entry)
    {
        try
        {
            using var stream = entry.GetStream();
            var converted = JsonSerializer.Deserialize(stream, SkyDefinitionSerializationContext.Default.SkyDefinitions) ?? throw new Exception("Data was null");
            Data = converted.Data;

            if (Data.FlatMapping != null)
            {
                foreach (var item in Data.FlatMapping)
                    FlatMapping[item.Flat] = item.Sky;
            }

            if (Data.Skies != null)
            {
                foreach (var sky in Data.Skies)
                {
                    if (!sky.Validate(out string error))
                        Log.Error(error);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, ParseUtil.GetParseError(entry, "skydefs", ex));
        }
    }
}