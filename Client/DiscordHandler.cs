using System;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Helion.Client;

public class DiscordHandler
{
    private const string AppId = "1367261916359299102";
    private DiscordRpcClient? m_client;

    private void Initialize()
    {
        if (m_client != null)
            return;
        m_client = new DiscordRpcClient(AppId);
        // m_client.Logger = new DiscordLogger(LogLevel.Warning);
        m_client.Initialize();
        UpdateRichPresence();
    }

    private void Dispose()
    {
        m_client?.Dispose();
        m_client = null;
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
            Initialize();
        else
            Dispose();
    }

    public void UpdateRichPresence(string? gameName = null, string? mapName = null)
    {
        if (m_client == null || !m_client.IsInitialized)
            return;

        m_client.SetPresence(new RichPresence()
        {
            Details = gameName,
            State = mapName,
        });
    }

    ~DiscordHandler() => Dispose();
}

internal class DiscordLogger(LogLevel level) : ILogger
{
    public LogLevel Level { get => m_level; set => m_level = value; }
    private LogLevel m_level = level;
    const string Template = "[Discord] {0}: {1}";

    public void Error(string message, params object[] args)
    {
        Console.WriteLine(string.Format(Template, "ERROR", message), args);
    }

    public void Warning(string message, params object[] args)
    {
        if (m_level > LogLevel.Warning)
            return;
        Console.WriteLine(string.Format(Template, "WARN", message), args);
    }

    public void Info(string message, params object[] args)
    {
        if (m_level > LogLevel.Info)
            return;
        Console.WriteLine(string.Format(Template, "INFO", message), args);
    }

    public void Trace(string message, params object[] args)
    {
        if (m_level > LogLevel.Trace)
            return;
        Console.WriteLine(string.Format(Template, "TRACE", message), args);
    }
}
