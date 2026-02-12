using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Discord;

public class DiscordIpcClient(string clientId) : IDisposable
{
    private readonly string m_clientId = clientId;
    private readonly int m_processId = Environment.ProcessId;
    private NamedPipeClientStream? m_pipe;
    private string? m_pipeName;
    private bool m_disposed;
    private readonly CancellationTokenSource canceller = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private async Task SendAsync<T>(DiscordMessageType type, T payload, CancellationToken cancellationToken) where T : DiscordMessage
    {
        if (m_pipe?.IsConnected != true)
            return;
        byte[] encodedPayload = EncodePayload(type, payload);
        await m_pipe.WriteAsync(encodedPayload, cancellationToken);
    }

    private void Send<T>(DiscordMessageType type, T payload) where T : DiscordMessage
    {
        if (m_pipe?.IsConnected != true)
            return;
        byte[] encodedPayload = EncodePayload(type, payload);
        m_pipe.Write(encodedPayload);
    }

    private static byte[] EncodePayload<T>(DiscordMessageType type, T payload) where T : DiscordMessage
    {
        int opcode = (int)type;
        string jsonPayload = JsonSerializer.Serialize(payload, typeof(T), DiscordSerializationContext.Default);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
        byte[] header = [.. BitConverter.GetBytes(opcode), .. BitConverter.GetBytes(payloadBytes.Length)];
        byte[] encodedPayload = [.. header, .. payloadBytes];
        return encodedPayload;
    }

    /// <remarks>
    /// We don't actually do anything with responses, so the JSON payload is not deserialized
    /// </remarks>
    private async Task<string?> ReceiveAsync(CancellationToken cancellationToken)
    {
        if (m_pipe?.IsConnected != true)
            return null;

        byte[] header = new byte[8];
        await m_pipe.ReadExactlyAsync(header, 0, 8, cancellationToken);
        int opcode = BitConverter.ToInt32(header, 0);
        int length = BitConverter.ToInt32(header, 4);
        if (opcode < 0 || opcode > 2 || length > 65536)
            return null;

        byte[] data = new byte[length];
        await m_pipe.ReadExactlyAsync(data, 0, length, cancellationToken);
        string jsonData = Encoding.UTF8.GetString(data);
        return jsonData;
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (m_pipe?.IsConnected == true)
            return;

        if (m_pipe != null)
            DisposePipe();

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        for (int i = 0; i <= 9; i++)
        {
            m_pipeName = $"discord-ipc-{i}";
            try
            {
                string? path = null;
                if (isWindows)
                    path = Path.Combine($@"\\.\pipe", m_pipeName);
                else
                {
                    string? runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
                    if (runtimeDir == null)
                    {
                        string? uid = Environment.GetEnvironmentVariable("UID");
                        if (uid != null)
                            runtimeDir = Path.Combine("/run/user", uid);
                    }
                    if (runtimeDir != null)
                        path = Path.Combine(runtimeDir, m_pipeName);
                }
                // check for file rather than throwing exception if missing
                if (!File.Exists(path))
                    continue;
                m_pipe = new NamedPipeClientStream(isWindows ? m_pipeName : path);
                await m_pipe.ConnectAsync(0, cancellationToken); // do not wait for missing pipe
                break;
            }
            catch (Exception e)
            {
                Log.Debug($"Discord: Failed to open pipe {m_pipeName}: {e.Message}");
                DisposePipe();
            }
        }
        if (m_pipe?.IsConnected != true)
        {
            Log.Debug($"Discord: Failed to open any pipe");
            return;
        }
        try
        {
            await SendAsync(DiscordMessageType.Connect, new DiscordConnectMessage { V = 1, ClientId = m_clientId }, cancellationToken);
            // We don't care about a response in most places, but we'll want the server to be in a ready state
            // before continuing and sending any status. Discord seems to have an anti-spam protection that can cause
            // this to take 15-20s if a connection is made multiple times in short period.
            await ReceiveAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Log.Debug($"Discord: Failed to connect: {e.Message}");
            DisposePipe();
        }
    }

    private void DisposePipe()
    {
        m_pipe?.Dispose();
        m_pipe = null;
        m_pipeName = null;
    }

    public DiscordCommandMessage CreateActivityMessage(bool playing, string? details = null, string? state = null)
    {
        return new DiscordCommandMessage()
        {
            Command = "SET_ACTIVITY",
            Args = new DiscordCommandArgs
            {
                Activity = playing
                    ? new DiscordActivity
                    {
                        Details = details,
                        State = state,
                    }
                    : null,
                ProcessId = m_processId
            },
            Nonce = Guid.NewGuid()
        };
    }

    private void Disconnect()
    {
        if (m_pipe?.IsConnected == true)
        {
            try
            {
                // need to clear activity before disconnecting, otherwise it remains after exit
                Send(DiscordMessageType.Activity, CreateActivityMessage(false));
                Send(DiscordMessageType.Disconnect, new DiscordDisconnectMessage());
            }
            catch (Exception e)
            {
                Log.Debug($"Discord: Failed to send disconnect message: {e.Message}");
            }
        }
        DisposePipe();
    }

    /// <summary>
    /// Connects to Discord if needed and then updates the current status.
    /// </summary>
    public async Task UpdateActivityAsync(string? details = null, string? state = null)
    {
        CancellationToken cancellationToken = canceller.Token;
        if (m_pipe?.IsConnected != true)
        {
            await ConnectAsync(cancellationToken);
        }
        try
        {
            DiscordCommandMessage payload = CreateActivityMessage(true, details, state);
            await SendAsync(DiscordMessageType.Activity, payload, cancellationToken);
        }
        catch (Exception e)
        {
            Log.Debug($"Discord: Failed to update rich presence: {e.Message}");
        }
    }

    void PerformDispose()
    {
        if (m_disposed)
            return;

        canceller.Cancel();
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
