using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Resources.Archives.Entries;
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
            Dictionary<string, string> options = Data?.Options ?? [];
            foreach (var item in newData.Options)
                options[item.Key] = item.Value;
            foreach (var item in options)
            {
                // since we may be raising the executable level and MBF21 force-disables
                // some options, we need to check options validity after merging
                if (!OptionValidForExecutable(item.Key, newData.Executable))
                    options.Remove(item.Key);
            }
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

    // https://doomwiki.org/wiki/OPTIONS
    private static bool OptionValidForExecutable(string option, string? executable)
    {
        if (executable == null || !ExecutableValues.TryGetValue(executable, out int val))
            return false;
        bool atLeastBugFixed = val >= ExecutableValues[GameConfConstants.Executable.BugFixed];
        bool atLeastBoom = val >= ExecutableValues[GameConfConstants.Executable.Boom2_02];
        bool atLeastComplevel9 = val >= ExecutableValues[GameConfConstants.Executable.Complevel9];
        bool atLeastMbf = val >= ExecutableValues[GameConfConstants.Executable.Mbf];
        bool atLeastMbf21 = val >= ExecutableValues[GameConfConstants.Executable.Mbf21];
        return option switch
        {
            // vanilla
            "comp_soul" => true,
            // Boom
            "comp_blazing" when atLeastBoom => true,
            "comp_doorlight" when atLeastBoom => true,
            "comp_doorstuck" when atLeastBoom => true,
            "comp_floors" when atLeastBoom => true,
            "comp_god" when atLeastBoom => true,
            "comp_model" when atLeastBoom => true,
            "comp_pain" when atLeastBoom => true,
            "comp_skull" when atLeastBoom => true,
            "comp_stairs" when atLeastBoom => true,
            "comp_vile" when atLeastBoom => true,
            "comp_zerotags" when atLeastBoom => true,
            // PrBoom
            "comp_zombie" when atLeastComplevel9 => true, // TODO: verify
            // MBF
            "comp_dropoff" when atLeastMbf => true,
            "comp_falloff" when atLeastMbf => true,
            "comp_infcheat" when atLeastMbf => true,
            "comp_pursuit" when atLeastMbf => true,
            "comp_respawn" when atLeastMbf => true,
            "comp_skymap" when atLeastMbf => true,
            "comp_staylift" when atLeastMbf => true,
            "comp_telefrag" when atLeastMbf => true,
            "dog_jumping" when atLeastMbf => true,
            "friend_distance" when atLeastMbf => true,
            "help_friends" when atLeastMbf => true,
            "monkeys" when atLeastMbf => true,
            "monster_avoid_hazards" when atLeastMbf => true,
            "monster_backing" when atLeastMbf => true,
            "monster_friction" when atLeastMbf => true,
            "monster_infighting" when atLeastMbf => true,
            "monsters_remember" when atLeastMbf => true,
            "player_helpers" when atLeastMbf => true,
            "weapon_recoil" when atLeastMbf => true,
            // MBF21
            "comp_friendlyspawn" when atLeastMbf21 => true,
            "comp_ledgeblock" when atLeastMbf21 => true,
            "comp_reservedlineflag" when atLeastMbf21 => true,
            "comp_voodooscroller" when atLeastMbf21 => true,

            // forced off in MBF21
            // vanilla
            "comp_666" when !atLeastMbf21 => true,
            "comp_maskedanim" when !atLeastMbf21 => true,
            // LxDoom
            "comp_moveblock" when atLeastBugFixed && !atLeastMbf21 => true, // TODO: verify
            // Boom
            "comp_maxhealth" when atLeastBoom && !atLeastMbf21 => true,
            "comp_sound" when atLeastBoom && !atLeastMbf21 => true,
            // PrBoom+
            "comp_ouchface" when atLeastComplevel9 && !atLeastMbf21 => true, // TODO: verify

            _ => false
        };
    }
}
