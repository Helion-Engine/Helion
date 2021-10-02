using Helion.Util.CommandLine;
using Helion.Util.Extensions;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Helion.Util.Loggers
{
    public static class HelionLoggers
    {
        public const string ErrorLoggerName = "error";
        public const string ProfilerLoggerName = "profiler";
        
        private static readonly ConsoleTarget ConsoleTarget = new("consoleTarget")
        {
            Layout = @"${message} ${exception}"
        };
        
        private static readonly DebuggerTarget DebuggerTarget = new("debuggerTarget")
        {
            Layout = "${message} ${exception}"
        };
        
        private static readonly FileTarget ErrorFileTarget = new("errorFileTarget")
        {
            FileName = "errorlog.txt",
            DeleteOldFileOnStartup = true,
            Layout = "${time} ${message} ${exception:format=ToString,StackTrace}"
        };
        
        private static readonly FileTarget LogFileTarget = new("logFileTarget")
        {
            // Note: The file name is overridden, but is here as a safeguard.
            FileName = "helion.log",
            DeleteOldFileOnStartup = true,
            Layout = "${time} [${level:uppercase=true}] ${message} ${exception}"
        };
        
        private static readonly FileTarget ProfilerFileTarget = new("profilerFileTarget")
        {
            // Note: The file name is overridden, but is here as a safeguard.
            FileName = "profiler.log",
            DeleteOldFileOnStartup = true,
            Layout = "${message}"
        };
        
        public static void Initialize(CommandLineArgs args)
        {
            LoggingConfiguration config = new();
            
            AddClassLoggers(config, args);
            AddErrorFileLogger(config);
            AddProfilerLogger(config, args);

            LogManager.Configuration = config;
        }

        private static void AddErrorFileLogger(LoggingConfiguration config)
        {
            config.AddTarget(ErrorFileTarget);
            config.AddRuleForAllLevels(ErrorFileTarget, ErrorLoggerName);
        }

        private static LogLevel GetMinLogLevel(CommandLineArgs args)
        {
            if (args.LogLevel != null)
            {
                if (args.LogLevel.EqualsIgnoreCase("debug"))
                    return LogLevel.Debug;
                if (args.LogLevel.EqualsIgnoreCase("trace"))
                    return LogLevel.Trace;
            }

            return LogLevel.Info;
        }

        private static void AddClassLoggers(LoggingConfiguration config, CommandLineArgs args)
        {
            LogLevel minLevel = GetMinLogLevel(args);

            config.AddTarget(ConsoleTarget);
            config.AddRule(minLevel, LogLevel.Fatal, ConsoleTarget, "Helion.*");
            
            config.AddTarget(DebuggerTarget);
            config.AddRule(minLevel, LogLevel.Fatal, DebuggerTarget, "Helion.*");

            if (args.LogFileName != null)
            {
                if (args.LogFileName != "")
                    LogFileTarget.FileName = args.LogFileName;
                
                config.AddTarget(LogFileTarget);
                config.AddRule(minLevel, LogLevel.Fatal, LogFileTarget, "Helion.*");
            }
        }

        private static void AddProfilerLogger(LoggingConfiguration config, CommandLineArgs args)
        {
            if (args.LogProfilerFileName == null)
                return;
            
            if (args.LogProfilerFileName != "")
                ProfilerFileTarget.FileName = args.LogProfilerFileName;
            
            config.AddTarget(ProfilerFileTarget);
            config.AddRuleForAllLevels(ProfilerFileTarget, ProfilerLoggerName);
        }
    }    
}
