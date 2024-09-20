using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Compatibility;
using Newtonsoft.Json;
using NLog;

namespace Helion.Resources.Definitions.Id24;

public class GameConfDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Dictionary<string, int> ExecutableValues = GameConfConstants.ValidExecutables
        .Select((x, i) => (x, i))
        .ToDictionary(pair => pair.x, pair => pair.i);
    private static readonly Dictionary<string, int> ModeValues = GameConfConstants.ValidModes
        .Select((x, i) => (x, i))
        .ToDictionary(pair => pair.x, pair => pair.i);
    public GameConfData? Data { get; set; } = null;

    public void Parse(Entry entry)
    {
        string data = entry.ReadDataAsString();
        try
        {
            var converted = JsonConvert.DeserializeObject<GameConf>(data)
                ?? throw new Exception($"Gameconf was null");
            var newData = converted.Data;

            if (converted.Type != GameConfConstants.Header.Type)
                throw new Exception($"Invalid header type: {converted.Type}");
            if (converted.Version != GameConfConstants.Header.Version)
                throw new Exception($"Unsupported header version: {converted.Type}");
            if (HasPath(newData.Iwad))
                throw new Exception($"Iwad has path: {newData.Iwad}");
            if (newData.Pwads != null)
            {
                foreach (string pwad in newData.Pwads)
                {
                    if (HasPath(pwad))
                        throw new Exception($"Pwad has path: {pwad}");
                }
            }

            // merge over previous GAMECONFs
            newData.Title ??= Data?.Title;
            newData.Author ??= Data?.Author;
            newData.Description ??= Data?.Description;
            newData.Version ??= Data?.Version;
            // TODO: iwads/pwads only seem relevant at program start and each GAMECONF
            // is considered individually; do they need to be merged here?
            newData.PlayerTranslations ??= Data?.PlayerTranslations;
            newData.WadTranslation ??= Data?.WadTranslation;
            newData.Executable = Max(ExecutableValues, newData.Executable, Data?.Executable);
            newData.Mode = Max(ModeValues, newData.Mode, Data?.Mode);

            // merge options
            Options options = Data?.Options ?? new();
            foreach (var item in newData.Options.Items)
                options.Set(item.Key, item.Value);
            newData.Options = options;

            Data = newData;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to parse gameconf from {entry.Parent.Path.Name}");
        }
    }

    /// <summary>
    /// WADs listed in a GAMECONF may not contain paths
    /// </summary>
    private static bool HasPath(string? name) => name != null && Path.GetFileName(name) != name;

    /// <summary>
    /// Gets the max of 2 string values based on a string->int value lookup.
    /// </summary>
    private static string? Max(Dictionary<string, int> valueLookup, string? a, string? b)
    {
        if (a == null || !valueLookup.TryGetValue(a, out int aValue))
            return b;
        if (b == null || !valueLookup.TryGetValue(b, out int bValue))
            return a;
        return (aValue > bValue) ? a : b;
    }
}
