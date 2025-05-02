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
        m_client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
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