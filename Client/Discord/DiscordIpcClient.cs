using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Discord;

public class DiscordIpcClient(string clientId) : IDisposable
{
    private readonly string m_clientId = clientId;
    private readonly int m_processId = Environment.ProcessId;
    private NamedPipeClientStream? m_pipe;
    private string? m_pipeName;
    public bool Connected => m_pipe?.IsConnected ?? false;
    private bool m_disposed;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private void Send<T>(DiscordMessageType type, T payload) where T : DiscordMessage
    {
        if (m_pipe == null || !Connected)
            return;

        int opcode = (int)type;
        string jsonPayload = JsonSerializer.Serialize(payload, typeof(T), DiscordSerializationContext.Default);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
        byte[] header = [.. BitConverter.GetBytes(opcode), .. BitConverter.GetBytes(payloadBytes.Length)];
        byte[] encodedPayload = [.. header, .. payloadBytes];
        m_pipe.Write(encodedPayload, 0, encodedPayload.Length);
    }

    /// <remarks>
    /// We don't actually do anything with responses, so the JSON payload is not deserialized
    /// </remarks>
    private string? Receive()
    {
        if (m_pipe == null || !Connected)
            return null;

        byte[] header = new byte[8];
        m_pipe.ReadExactly(header, 0, 8);
        int opcode = BitConverter.ToInt32(header, 0);
        if (opcode < 0 || opcode > 2)
            return null;
        int length = BitConverter.ToInt32(header, 4);

        byte[] data = new byte[length];
        m_pipe.ReadExactly(data, 0, length);
        string jsonData = Encoding.UTF8.GetString(data);
        return jsonData;
    }

    public void Connect()
    {
        if (Connected)
            return;

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        for (int i = 0; i <= 9; i++)
        {
            m_pipeName = $"discord-ipc-{i}";
            try
            {
                string path = isWindows ? $@"\\.\pipe\{m_pipeName}" : $"/tmp/{m_pipeName}";
                // check for file rather than throwing exception if missing
                if (!File.Exists(path))
                    continue;
                m_pipe = new NamedPipeClientStream(m_pipeName);
                m_pipe.Connect(0); // do not wait for missing pipe
                break;
            }
            catch (Exception e)
            {
                Log.Info($"Discord: Failed to open pipe {m_pipeName}: {e.Message}");
                DisposePipe();
            }
        }
        if (!Connected)
        {
            Log.Info($"Discord: Failed to open any pipe");
            return;
        }
        try
        {
            Send(DiscordMessageType.Connect, new DiscordConnectMessage { V = 1, ClientId = m_clientId });
            // Receive();
        }
        catch (Exception e)
        {
            Log.Info($"Discord: Failed to connect: {e.Message}");
            DisposePipe();
        }
    }

    private void DisposePipe()
    {
        m_pipe?.Dispose();
        m_pipe = null;
        m_pipeName = null;
    }

    public void Disconnect()
    {
        if (Connected)
        {
            try
            {
                Send(DiscordMessageType.Disconnect, new DiscordDisconnectMessage());
                // Receive();
            }
            catch (Exception e)
            {
                Log.Info($"Discord: Failed to send disconnect message: {e.Message}");
            }
        }
        DisposePipe();
    }

    public void UpdateActivity(string? details = null, string? state = null)
    {
        try
        {
            DiscordCommandMessage payload = new()
            {
                Command = "SET_ACTIVITY",
                Args = new DiscordCommandArgs
                {
                    Activity = new DiscordActivity
                    {
                        Details = details,
                        State = state,
                    },
                    ProcessId = m_processId
                },
                Nonce = Guid.NewGuid()
            };

            Send(DiscordMessageType.Activity, payload);
            // Receive();
        }
        catch (Exception e)
        {
            Log.Info($"Discord: Failed to update rich presence: {e.Message}");
        }
    }

    void PerformDispose()
    {
        if (m_disposed)
            return;

        Disconnect();
        m_disposed = true;
    }

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    ~DiscordIpcClient()
    {
        FailedToDispose(this);
        PerformDispose();
    }
}
