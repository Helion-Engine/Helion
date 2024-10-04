namespace Helion.Resources.Definitions.Id24;
using System.Text.Json.Serialization;

public class TranslationData
{
    public string Name { get; set; } = string.Empty;
    public string SBarBack { get; set; } = string.Empty;
    public bool SBarTranslate { get; set; }
    public string InterBack { get; set; } = string.Empty;
    [JsonConverter(typeof(ByteArrayConverter))]
    public bool InterTranslate { get; set; }
    public byte[] Table { get; set; } = [];
}

public class TranslationDef
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public TranslationData Data { get; set; } = new();
}
