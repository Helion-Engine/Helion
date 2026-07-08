using Helion.Resources.Archives.Entries;
using Helion.Util.SerializationContexts;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Helion.Resources.Definitions.Id24;

public enum Id24TrackInfoType
{
    [Description("None")]
    None,
    [Description("MIDI")]
    Midi,
    [Description("Remixed")]
    Remixed,
}

public class Id24TrackInfoDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Dictionary<string, Id24TrackInfo> TrackInfoData { get; set; } = [];

    public void Parse(Entry entry)
    {
        try
        {
            using var stream = entry.GetStream();
            var trackInfo = (Dictionary<string, Id24TrackInfo>?)JsonSerializer.Deserialize(stream, TrackInfoSerializationContext.Default.Id24TrackInfoMap) ?? throw new Exception("Data was null");
            foreach (var kvp in trackInfo)
            {
                if (kvp.Value.IsValid())
                    TrackInfoData[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, ParseUtil.GetParseError(entry, "TRAKINFO", ex));
        }
    }

    public bool TryGetTrackInfo(string sha1, Id24TrackInfoType type, [NotNullWhen(true)] out string? trackName)
    {
        trackName = null;
        if (!TrackInfoData.TryGetValue(sha1, out var trackInfo))
            return false;

        switch (type)
        {
            case Id24TrackInfoType.Midi:
                trackName = trackInfo.Midi;
                break;
            case Id24TrackInfoType.Remixed:
                trackName = trackInfo.Remixed;
                break;
        }

        return trackName != null;
    }
}
