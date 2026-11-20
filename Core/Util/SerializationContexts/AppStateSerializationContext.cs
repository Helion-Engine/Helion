namespace Helion.Util.SerializationContexts;

using Helion.Resources.Definitions.Id24;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
    PropertyNameCaseInsensitive = true,
    IncludeFields = true)]
[JsonSerializable(typeof(EnvironmentAppState), TypeInfoPropertyName = "EnvironmentAppState")]
public partial class EnvironmentAppStateSerializationContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
    PropertyNameCaseInsensitive = true,
    IncludeFields = true)]
[JsonSerializable(typeof(InstallAppState), TypeInfoPropertyName = "InstallAppState")]
public partial class InstallAppStateSerializationContext : JsonSerializerContext
{
}
