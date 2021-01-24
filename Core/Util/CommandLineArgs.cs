using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Util
{
    /// <summary>
    /// A collection of all the command line arguments we support, which have
    /// its fields populated by the command line parsing library.
    /// </summary>
    public class CommandLineArgs
    {
        public bool ErrorWhileParsing { get; set; }
        public bool NoWriteToConsole { get; set; }
        public bool TimestampLogFile { get; set; }

        public List<string> Files { get; set; } = new List<string>();
        public string? Iwad { get; set; }
        public string? Map { get; set; }
        public string? LogPath { get; set; }
        public string? Warp { get; set; }
        public int? Skill { get; set; }
        public bool NoMonsters { get; set; }
        public bool SV_FastMonsters { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Parses the command line arguments and returns an object with the
        /// parsed results.
        /// </summary>
        /// <param name="args">The args to parse.</param>
        /// <returns>The command line argument results.</returns>
        public static CommandLineArgs Parse(string[] args)
        {
            CommandLineArgs commandLineArgs = new CommandLineArgs();
            CommandParser parser = new CommandParser(new[] { "-", "+" });
            List<CommandArg> parsedArgs = parser.Parse(args);

            foreach (var parsedArg in parsedArgs)
            {
                if (IsArgMatch(parsedArg, "-iwad"))
                    commandLineArgs.Iwad = GetString(commandLineArgs, parsedArg);
                else if (IsArgMatch(parsedArg, "-file"))
                    commandLineArgs.Files.AddRange(parsedArg.Values);
                else if (IsArgMatch(parsedArg, "-log"))
                    commandLineArgs.LogPath = GetString(commandLineArgs, parsedArg);
                else if (IsArgMatch(parsedArg, "-warp"))
                    commandLineArgs.Warp = GetConcatString(commandLineArgs, parsedArg, " ");
                else if (IsArgMatch(parsedArg, "-skill"))
                    commandLineArgs.Skill = ParseInt(commandLineArgs, parsedArg);
                else if (IsArgMatch(parsedArg, "+map"))
                    commandLineArgs.Map = GetString(commandLineArgs, parsedArg);
                else if (IsArgMatch(parsedArg, "-nomonsters"))
                    commandLineArgs.NoMonsters = true;
                else if (IsArgMatch(parsedArg, "+sv_fastmonsters"))
                    commandLineArgs.SV_FastMonsters = GetBoolArg(parsedArg);
                else
                    commandLineArgs.Errors.Add("Unknown command: " + parsedArg.Key);
            }

            return commandLineArgs;
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
}
