using Helion.Geometry.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Util.CommandLine;

/// <summary>
/// A collection of all the command line arguments we support, which have
/// its fields populated by the command line parsing library.
/// </summary>
public class CommandLineArgs
{
    public string[] OriginalArgs = Array.Empty<string>();
    public readonly List<string> Errors = new();
    public readonly List<string> Files = new();
    public string? Iwad { get; set; }
    public string? Map { get; set; }
    public string? LogFileName { get; set; }
    public string? LogLevel { get; set; }
    public string? LogProfilerFileName { get; set; }
    public string? Warp { get; set; }
    public int? Skill { get; set; }
    public bool NoMonsters { get; set; }
    public bool LevelStat { get; set; }
    public string? LoadGame { get; set; }
    public bool SV_FastMonsters { get; set; }
    public string? DehackedPatch { get; set; }
    public string? Record { get; set; }
    public string? PlayDemo { get; set; }
    public Vec3D? SetPosition { get; set; }
    public double? SetAngle { get; set; }
    public double? SetPitch { get; set; }
    public IList<string> Cheats { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Parses the command line arguments and returns an object with the
    /// parsed results.
    /// </summary>
    /// <param name="args">The args to parse.</param>
    /// <returns>The command line argument results.</returns>
    public static CommandLineArgs Parse(string[] args)
    {
        CommandLineArgs commandLineArgs = new() { OriginalArgs = args };
        var argStart = new[] { "-", "+" };

        // Drag and drop files will be specified as the file. Assume anything in front with -/+ is a file.
        foreach (var arg in args)
        {
            if (argStart.Any(x => arg.StartsWith(x)))
                break;

            commandLineArgs.Files.Add(arg);
        }

        CommandParser parser = new(argStart);
        List<CommandArg> parsedArgs = parser.Parse(args);

        foreach (var parsedArg in parsedArgs)
        {
            if (IsArgMatch(parsedArg, "-iwad"))
                commandLineArgs.Iwad = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "-file"))
                commandLineArgs.Files.AddRange(parsedArg.Values);
            else if (IsArgMatch(parsedArg, "-log"))
                commandLineArgs.LogFileName = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "-loglevel"))
                commandLineArgs.LogLevel = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "-logprofiler"))
                commandLineArgs.LogProfilerFileName = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "-warp"))
                commandLineArgs.Warp = GetConcatString(commandLineArgs, parsedArg, " ");
            else if (IsArgMatch(parsedArg, "-skill"))
                commandLineArgs.Skill = ParseInt(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "+map"))
                commandLineArgs.Map = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "-nomonsters"))
                commandLineArgs.NoMonsters = true;
            else if (IsArgMatch(parsedArg, "-levelstat"))
                commandLineArgs.LevelStat = true;
            else if (IsArgMatch(parsedArg, "-loadgame"))
                commandLineArgs.LoadGame = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "+sv_fastmonsters"))
                commandLineArgs.SV_FastMonsters = GetBoolArg(parsedArg);
            else if (IsArgMatch(parsedArg, "-deh"))
                commandLineArgs.DehackedPatch = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "-record"))
                commandLineArgs.Record = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "-playdemo"))
                commandLineArgs.PlayDemo = GetString(commandLineArgs, parsedArg);
            else if (IsArgMatch(parsedArg, "+setpos"))
                commandLineArgs.SetPosition = GetPosition(GetString(commandLineArgs, parsedArg));
            else if (IsArgMatch(parsedArg, "+cheats"))
                commandLineArgs.Cheats = ParseCheats(GetString(commandLineArgs, parsedArg));
            else if (IsArgMatch(parsedArg, "+setangle"))
                commandLineArgs.SetAngle = ParseDouble(GetString(commandLineArgs, parsedArg));
            else if (IsArgMatch(parsedArg, "+setpitch"))
                commandLineArgs.SetPitch = ParseDouble(GetString(commandLineArgs, parsedArg));
            else
                commandLineArgs.Errors.Add("Unknown command: " + parsedArg.Key);
        }

        return commandLineArgs;
    }

    private static double? ParseDouble(string? value)
    {
        if (!double.TryParse(value, out var dValue))
            return null;

        return dValue;
    }

    private static IList<string> ParseCheats(string? str)
    {
        if (str == null)
            return Array.Empty<string>();

        return str.Split(new char[] { ' ' });
    }

    private static Vec3D? GetPosition(string value)
    {
        string[] items = value.Split(new char[] { ',' });
        if (items.Length < 3)
            return null;
        if (!double.TryParse(items[0], out var x) || !double.TryParse(items[1], out var y) || !double.TryParse(items[2], out var z))
            return null;

        return new Vec3D(x, y, z);
    }

    private static bool IsArgMatch(CommandArg arg, string str) => arg.Key.Equals(str, StringComparison.OrdinalIgnoreCase);

    private static string? GetConcatString(CommandLineArgs commandLineArgs, CommandArg arg, string separator)
    {
        if (arg.Values.Count == 0)
        {
            commandLineArgs.Errors.Add($"No parameter specified for {arg.Key}");
            return null;
        }

        return string.Join(separator, arg.Values);
    }

    private static string? GetString(CommandLineArgs commandLineArgs, CommandArg arg)
    {
        if (arg.Values.Count == 0)
        {
            commandLineArgs.Errors.Add($"No parameter specified for {arg.Key}");
            return null;
        }

        return arg.Values.First();
    }

    private static bool GetBoolArg(CommandArg arg)
    {
        if (arg.Values.Count == 0)
            return false;

        return arg.Values[0] != "0";
    }

    private static int? ParseInt(CommandLineArgs commandLineArgs, CommandArg arg)
    {
        if (arg.Values.Count == 0)
            return null;

        if (int.TryParse(arg.Values.First(), out int value))
            return value;

        commandLineArgs.Errors.Add($"Invalid {arg.Key}: {arg.Values.First()}");
        return null;
    }
}
