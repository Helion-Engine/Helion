using System;
using Helion.Util.CommandLine;
using Helion.Util.Extensions;
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
        public static void Initialize(CommandLineArgs args)
        {
            LoggingConfiguration config = new();
            SetupConsole(config, args);
            SetupDebugger(config, args);
            SetupFileLogger(config, args);

            LogManager.Configuration = config;
        }
        
        private static void SetupConsole(LoggingConfiguration config, CommandLineArgs args)
        {
            ConsoleTarget consoleTarget = new("console")
            {
                Layout = @"${message} ${exception}",
            };
            AddRuleForAppropriateLevels(config, consoleTarget, args);
            config.AddTarget(consoleTarget);
        }

        private static void SetupDebugger(LoggingConfiguration config, CommandLineArgs args)
        {
            DebuggerTarget debuggerTarget = new("debugger")
            {
                Layout = "${message} ${exception}",
            };
            AddRuleForAppropriateLevels(config, debuggerTarget, args);
            config.AddTarget(debuggerTarget);
        }

        private static void SetupFileLogger(LoggingConfiguration config, CommandLineArgs args)
        {
            if (string.IsNullOrEmpty(args.LogPath))
                return;

            FileTarget fileTarget = new("file")
            {
                FileName = $"helion_{DateTime.Now:o}_{args.LogPath}",
                Layout = "${time} [${level:uppercase=true}] ${message} ${exception}",
            };
            AddRuleForAppropriateLevels(config, fileTarget, args);
            config.AddTarget(fileTarget);
        }

        public static void AddRuleForAppropriateLevels(LoggingConfiguration config, Target target, CommandLineArgs args)
        {
            config.AddRuleForOneLevel(LogLevel.Off, target);
            config.AddRuleForOneLevel(LogLevel.Fatal, target);
            config.AddRuleForOneLevel(LogLevel.Error, target);
            config.AddRuleForOneLevel(LogLevel.Warn, target);
            config.AddRuleForOneLevel(LogLevel.Info, target);

#if DEBUG
            string level = args.LogLevel ?? "";
            
            bool debugLevel = level.EqualsIgnoreCase("debug");
            if (debugLevel)
                config.AddRuleForOneLevel(LogLevel.Debug, target);
            
            bool traceLevel = level.EqualsIgnoreCase("trace") || debugLevel;
            if (traceLevel)
                config.AddRuleForOneLevel(LogLevel.Trace, target);
#endif
        }
    }
}
