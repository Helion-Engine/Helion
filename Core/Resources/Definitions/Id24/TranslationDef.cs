namespace Helion.Resources.Definitions.Id24;

public class TranslationData
{
    public string Name { get; set; } = string.Empty;
    public string SBarBack { get; set; } = string.Empty;
    public string SBarTranslate { get; set; } = string.Empty;
    public string InterBack { get; set; } = string.Empty;
    public byte[] Table { get; set; } = [];
}

public class TranslationDef
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public TranslationData Data { get; set; }
}
