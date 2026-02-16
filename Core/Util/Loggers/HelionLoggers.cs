using System.IO;
using Helion.Util.CommandLine;
using Helion.Util.Extensions;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Helion.Util.Loggers;

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

    public static void Initialize(CommandLineArgs args, string userDataFolder)
    {
        LoggingConfiguration config = new();

        AddClassLoggers(config, args, userDataFolder);
        AddErrorFileLogger(config, userDataFolder);
        AddProfilerLogger(config, args, userDataFolder);

        LogManager.Configuration = config;
    }

    private static void AddErrorFileLogger(LoggingConfiguration config, string userDataFolder)
    {
        FileTarget ErrorFileTarget = new("errorFileTarget")
        {
            // Note: The file name is overridden, but is here as a safeguard.
            FileName = Path.Combine(userDataFolder, "errorlog.txt"),
            DeleteOldFileOnStartup = true,
            Layout = "${time} ${message} ${exception:format=ToString,StackTrace}"
        };

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

    private static void AddClassLoggers(LoggingConfiguration config, CommandLineArgs args, string userDataFolder)
    {
        FileTarget LogFileTarget = new("logFileTarget")
        {
            // Note: The file name is overridden, but is here as a safeguard.
            FileName = Path.Combine(userDataFolder, "helion.log"),
            DeleteOldFileOnStartup = true,
            Layout = "${time} [${level:uppercase=true}] ${message} ${exception}"
        };

        LogLevel minLevel = GetMinLogLevel(args);

        config.AddTarget(ConsoleTarget);
        config.AddRule(minLevel, LogLevel.Fatal, ConsoleTarget, "Helion.*");

        config.AddTarget(DebuggerTarget);
        config.AddRule(minLevel, LogLevel.Fatal, DebuggerTarget, "Helion.*");

        if (args.LogFileName != null)
        {
            if (args.LogFileName != "")
                LogFileTarget.FileName = GetLogFilePath(userDataFolder, args.LogFileName);

            config.AddTarget(LogFileTarget);
            config.AddRule(minLevel, LogLevel.Fatal, LogFileTarget, "Helion.*");
        }
    }

    private static void AddProfilerLogger(LoggingConfiguration config, CommandLineArgs args, string userDataFolder)
    {
        if (args.LogProfilerFileName == null)
            return;

        FileTarget ProfilerFileTarget = new("profilerFileTarget")
        {
            // Note: The file name is overridden, but is here as a safeguard.
            FileName = Path.Combine(userDataFolder, "profiler.log"),
            DeleteOldFileOnStartup = true,
            Layout = "${message}"
        };

        if (args.LogProfilerFileName != "")
            ProfilerFileTarget.FileName = GetLogFilePath(userDataFolder, args.LogProfilerFileName);

        config.AddTarget(ProfilerFileTarget);
        config.AddRuleForAllLevels(ProfilerFileTarget, ProfilerLoggerName);
    }

    private static string GetLogFilePath(string userDataFolder, string filePath)
    {
        // If the user specified a directory then use it
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            return filePath;

        return Path.Combine(userDataFolder, filePath);
    }
}
