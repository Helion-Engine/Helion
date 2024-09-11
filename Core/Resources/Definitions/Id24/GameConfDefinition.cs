using System;
using System.IO;
using Helion.Resources.Archives.Entries;
using Newtonsoft.Json;
using NLog;

namespace Helion.Resources.Definitions.Id24;

public class GameConfDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public GameConfData? Data { get; set; } = null;

    public void Parse(Entry entry)
    {
        string data = entry.ReadDataAsString();
        try
        {
            var converted = JsonConvert.DeserializeObject<GameConf>(data)
                ?? throw new Exception($"Gameconf was null");

            if (converted.Type != GameConfConstants.Header.Type)
                throw new Exception($"Invalid header type: {converted.Type}");
            if (converted.Version != GameConfConstants.Header.Version)
                throw new Exception($"Unsupported header version: {converted.Type}");
            if (HasPath(converted.Data.Iwad))
                throw new Exception($"Iwad has path: {converted.Data.Iwad}");
            if (converted.Data.Pwads != null)
            {
                foreach (string pwad in converted.Data.Pwads)
                {
                    if (HasPath(pwad))
                        throw new Exception($"Pwad has path: {pwad}");
                }
            }

            Data = converted.Data;
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
}
