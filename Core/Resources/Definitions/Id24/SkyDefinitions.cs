using System.Collections.Generic;

namespace Helion.Resources.Definitions.Id24;

public class SkyFlatMapping
{
    public string Flat { get; set; } = string.Empty;
    public string Sky { get; set; } = string.Empty;
}

public class SkyDefinitionData
{
    public List<SkyDef> Skies { get; set; } = [];
    public List<SkyFlatMapping>? FlatMapping { get; set; } = [];
}

public class SkyDefinitions
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public SkyDefinitionData Data { get; set; } = new();
}
