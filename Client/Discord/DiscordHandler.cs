using System;
using System.Threading.Tasks;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Discord;

public class DiscordHandler : IDisposable
{
    private const string AppId = "1367261916359299102";
    private DiscordIpcClient? m_client;
    private bool m_disposed;

    public void SetEnabled(bool enabled)
    {
        if (enabled)
            m_client ??= new DiscordIpcClient(AppId);
        else
        {
            m_client?.Dispose();
            m_client = null;
        }
    }

    public void UpdateRichPresence(string? gameName = null, string? mapName = null)
    {
        if (m_client == null)
            return;
        Task.Run(() => m_client.UpdateActivityAsync(gameName, mapName));
    }

    void PerformDispose()
    {
        if (m_disposed)
            return;

        m_client?.Dispose();
        m_disposed = true;
    }

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    ~DiscordHandler()
    {
        FailedToDispose(this);
        PerformDispose();
    }
}
