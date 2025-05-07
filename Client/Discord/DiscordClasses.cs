using System;
using System.Text.Json.Serialization;

namespace Helion.Client.Discord;

public enum DiscordMessageType
{
    Connect = 0,
    Activity = 1,
    Disconnect = 2
}

public abstract class DiscordMessage { }

public class DiscordConnectMessage : DiscordMessage
{
    [JsonPropertyName("v")]
    public required int V { get; set; }
    [JsonPropertyName("client_id")]
    public required string ClientId { get; set; }
}

public class DiscordDisconnectMessage : DiscordMessage { }

public class DiscordCommandMessage : DiscordMessage
{
    [JsonPropertyName("cmd")]
    public required string Command { get; set; }
    [JsonPropertyName("args")]
    public required DiscordCommandArgs Args { get; set; }
    [JsonPropertyName("nonce")]
    public required Guid Nonce { get; set; }
}

public class DiscordCommandArgs
{
    [JsonPropertyName("activity")]
    public DiscordActivity? Activity { get; set; }
    [JsonPropertyName("pid")]
    public required int ProcessId { get; set; }
}

public class DiscordActivity
{
    [JsonPropertyName("details")]
    public string? Details { get; set; }
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
    PropertyNameCaseInsensitive = true,
    IncludeFields = true)]
[JsonSerializable(typeof(DiscordConnectMessage), TypeInfoPropertyName = nameof(DiscordConnectMessage))]
[JsonSerializable(typeof(DiscordDisconnectMessage), TypeInfoPropertyName = nameof(DiscordDisconnectMessage))]
[JsonSerializable(typeof(DiscordCommandMessage), TypeInfoPropertyName = nameof(DiscordCommandMessage))]

public partial class DiscordSerializationContext : JsonSerializerContext { }
