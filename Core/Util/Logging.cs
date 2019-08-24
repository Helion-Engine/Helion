using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Helion.Util
{
    /// <summary>
    /// Handles all logging instantiation and setup.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Initializes logging through the command line arguments options.
        /// </summary>
        /// <param name="commandLineArgs">The command line arguments.</param>
        public static void Initialize(CommandLineArgs commandLineArgs)
        {
            LoggingConfiguration config = new LoggingConfiguration();
            SetupConsole(config, commandLineArgs);
            SetupDebugger(config);
            SetupFileLogger(config, commandLineArgs);

            LogManager.Configuration = config;
        }
        
        private static void SetupConsole(LoggingConfiguration config, CommandLineArgs commandLineArgs)
        {
            if (commandLineArgs.NoWriteToConsole)
                return;

            ConsoleTarget consoleTarget = new ConsoleTarget("console")
            {
                Layout = @"${message} ${exception}",
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
        }

        private static void SetupDebugger(LoggingConfiguration config)
        {
            DebuggerTarget debuggerTarget = new DebuggerTarget("debugger")
            {
                Layout = "${message} ${exception}",
            };
            config.AddTarget(debuggerTarget);
            config.AddRuleForAllLevels(debuggerTarget);
        }

        private static void SetupFileLogger(LoggingConfiguration config, CommandLineArgs commandLineArgs)
        {
            if (string.IsNullOrEmpty(commandLineArgs.LogPath))
                return;

            string logFile = commandLineArgs.LogPath;
            if (commandLineArgs.TimestampLogFile)
                logFile = $"helion_{DateTime.Now:o}_{logFile}";

            FileTarget fileTarget = new FileTarget("file")
            {
                FileName = logFile,
                Layout = "${longdate} ${level:uppercase=true} ${logger}: ${message} ${exception}",
            };
            config.AddTarget(fileTarget);
            config.AddRuleForAllLevels(fileTarget);
        }
    }
}
