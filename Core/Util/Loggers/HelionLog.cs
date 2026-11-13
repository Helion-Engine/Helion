using System;
using System.Diagnostics;

namespace Helion.Util.Loggers;

public enum MessageLevel
{
    Debug,
    Trace,
    Info,
    Warning,
    Error,
}

public readonly record struct MessageLogEvent(string Message, MessageLevel Level);

public static class HelionLog
{
    public static event EventHandler<MessageLogEvent>? Message;

    public static void Info(string message) => Message?.Invoke(null, new(message, MessageLevel.Info));
    public static void Warn(string message) => Message?.Invoke(null, new(message, MessageLevel.Warning));
    public static void Error(string message) => Message?.Invoke(null, new(message, MessageLevel.Error));
    [Conditional("DEBUG")]
    public static void Debug(string message) => Message?.Invoke(null, new(message, MessageLevel.Debug));
}
