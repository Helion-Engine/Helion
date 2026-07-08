using Helion.Resources.Definitions.Id24;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helion.Util.SerializationContexts;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
    PropertyNameCaseInsensitive = true,
    IncludeFields = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(Dictionary<string, Id24TrackInfo>), TypeInfoPropertyName = "Id24TrackInfoMap")]
[JsonSerializable(typeof(Id24TrackInfo))]
public partial class TrackInfoSerializationContext : JsonSerializerContext
{
}
