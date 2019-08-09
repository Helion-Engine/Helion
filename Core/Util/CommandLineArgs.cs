using System.Collections.Generic;
using CommandLine;

namespace Helion.Util
{
    /// <summary>
    /// A collection of all the command line arguments we support, which have
    /// its fields populated by the command line parsing library.
    /// </summary>
    public class CommandLineArgs
    {
        /// <summary>
        /// Set to true if there was an error while parsing.
        /// </summary>
        public bool ErrorWhileParsing = false;

        [Option('f', "file", Required = false, HelpText = "A list of files to load")]
        public IList<string> Files { get; set; } = new List<string>();

        [Option('l', "log", Required = false, HelpText = "The name of the log file")]
        public string LogPath { get; set; } = "";

        [Option]
        public bool NoConsole { get; set; } = false;

        [Option]
        public bool TimestampLogFile { get; set; } = false;

        [Option("warp", Required = false, HelpText = "The level to warp to")]
        public int? Warp { get; set; }

        private static void HandleParsingError(CommandLineArgs result)
        {
            result.ErrorWhileParsing = true;
            System.Console.WriteLine("Unable to parse command line args.");
            System.Console.WriteLine("Values that would be set by the command line args will be defaulted values!");
        }

        /// <summary>
        /// Parses the command line arguments and returns an object with the
        /// parsed results.
        /// </summary>
        /// <param name="args">The args to parse.</param>
        /// <returns>The command line argument results.</returns>
        public static CommandLineArgs Parse(string[] args)
        {
            CommandLineArgs result = new CommandLineArgs();

            try
            {
                CommandLine.Parser.Default
                    .ParseArguments<CommandLineArgs>(args)
                    .WithParsed(cmdArgs => result = cmdArgs)
                    .WithNotParsed(_ => HandleParsingError(result));
            }
            catch
            {
                HandleParsingError(result);
            }

            return result;
        }
    }
}
